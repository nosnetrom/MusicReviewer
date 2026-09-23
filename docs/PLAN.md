# MusicReviewer — Application Plan

> Status: **v0.3**. Decisions are recorded in [§10](#10-decisions). **Phases 0 (scaffold) and 1 (Glass UI shell) are complete**; Phase 2 (catalog) is next.

## 1. Product summary

MusicReviewer lets a visitor search for a famous musician or band, see a curated list of their **major recordings**, and read a short **summary** of each recording taken from Wikipedia.

**v1** is a public, read-only catalogue of popular music, jazz, folk and so on. Classical music is out of scope for v1.
**v2** adds user accounts, follows, user reviews and email.

### Core user stories (v1)
1. **Search**: type "Miles Davis" and get matching artists (with disambiguation, e.g. "Genesis (UK band)").
2. **Artist page**: bio snippet (from Wikipedia), photo, genres, active years, and a ranked list of major recordings (studio albums first, with filters for live, compilations, EPs).
3. **Recording page**: cover art, release date, label, track list, personnel, and a **Wikipedia summary** with attribution and a link to the full article.
4. **Browse**: featured artists, recent additions, browse by genre or decade.

### v2 stories
5. Accounts: sign up/in (Entra External ID), follow artists, save favourites.
6. User reviews and star ratings on recordings.
7. Email (Azure Communication Services): welcome email, weekly digest for followed artists, review-reply notifications.
8. Admin: curate featured artists, pin or hide recordings, moderate reviews.

---

## 2. Technology stack

| Layer | Choice | Notes |
|---|---|---|
| Front end | **Vue 3.5 + Vite, JavaScript** (no TS), Vue Router, Pinia | `<script setup>` SFCs; JSDoc for type hints |
| UI styling | Custom **Liquid Glass design system** (plain CSS + CSS custom properties) | No heavy UI kit; see §6 |
| Front-end testing | Vitest + Vue Test Utils; Playwright for E2E | |
| Runtime | **Node 24 LTS** | Pinned in `.nvmrc` and in CI |
| Back end | **ASP.NET Core Web API (C#) on .NET 10 LTS**, Minimal APIs grouped by feature | SDK pinned in `global.json` |
| Data access | EF Core 10 + SQL Server provider, code-first migrations | |
| Background work | `BackgroundService` with a queue for ingestion and refresh | |
| Back-end testing | xUnit, `WebApplicationFactory` integration tests | |
| Database | **Azure SQL Database** (serverless tier for dev and test) | SQL Server LocalDB for local dev |
| Email *(v2)* | **Azure Communication Services Email** | Custom domain, managed identity |
| Auth *(v2)* | **Microsoft Entra External ID** | JWT bearer on the API |
| Hosting | **Azure Static Web Apps** (Vue) + **Azure App Service** (API) | |
| Secrets | **Azure Key Vault** + managed identity | |
| Observability | **Application Insights** | Front end and API |
| IaC / CI | **Bicep** + **GitHub Actions** | |

---

## 3. Where the music data comes from

We **ingest from open sources and cache what we get in Azure SQL**.

| Source | Used for | Licence and limits |
|---|---|---|
| **MusicBrainz API** | Artists, release groups (albums), releases, tracks, credits, MBIDs as stable keys | Core data is CC0. **1 request/sec**, and a descriptive User-Agent is required |
| **Cover Art Archive** | Album artwork (served from its CDN) | Free, keyed by MBID |
| **Wikipedia (REST + Action API)** | Recording summaries, artist bios, images | CC BY-SA 4.0, so attribution and a link to the article are required |
| **Wikidata** | Linking MusicBrainz MBIDs to Wikipedia articles (`P434`/`P436`) | CC0 |

### What counts as a "major recording"
- MusicBrainz primary type = Album, with no secondary type (so compilations, live albums, soundtracks and remixes are excluded from the default view).
- The recording has its own English Wikipedia article (the strongest notability signal).
- MusicBrainz community rating, if there is one.
- Manual pin or hide (admin, v2).

### Ingestion flow
1. Search queries the local DB first. On a miss, it queries MusicBrainz live, rate-limited through one shared `HttpClient` with a token-bucket limiter and Polly retries.
2. When an artist page is opened, a background job pulls the discography, cover-art URLs, Wikidata links and Wikipedia extracts, then stores them.
3. A nightly refresh updates stale artists (older than 30 days).

---

## 4. Summaries (Wikipedia)

- Recordings are linked to Wikipedia through the MusicBrainz release group, then its Wikidata item, then the `enwiki` sitelink. If no Wikidata link exists, we fall back to a title search.
- The summary text is the article's **lead section** as plain text, fetched from the `/page/summary` REST endpoint (the short version) or from `action=query&prop=extracts&exintro` (the full lead).
- We store the extract, the article URL, the revision ID and the fetched-at time. Extracts are refreshed when the stored revision is out of date.
- The UI shows: *"Summary from Wikipedia, licensed under CC BY-SA 4.0"* with a link to the article.
- When a recording has no article, the page shows its MusicBrainz facts only, with a "No Wikipedia summary available" note.

---

## 5. Architecture

```
┌──────────────────────────┐        ┌───────────────────────────────────────┐
│  Vue SPA (Static Web App)│  HTTPS │  ASP.NET Core API (App Service)       │
│  Router · Pinia · Glass  ├───────►│  Minimal API endpoints → services     │
│  UI                      │        │  ├─ EF Core ──────► Azure SQL         │
└──────────────────────────┘        │  ├─ MusicBrainz / CAA / Wikidata /    │
                                    │  │  Wikipedia clients                 │
                                    │  ├─ ACS Email (v2)                    │
                                    │  └─ Key Vault · App Insights          │
                                    └───────────────────────────────────────┘
```

### Solution layout
```
MusicReviewer/
├─ MusicReviewer.slnx
├─ global.json  .nvmrc  .editorconfig  .gitignore  Directory.Build.props
├─ src/
│  ├─ MusicReviewer.Api/             # ASP.NET Core host: endpoints, DI, OpenAPI, CORS
│  ├─ MusicReviewer.Application/     # Use cases, DTOs, interfaces
│  ├─ MusicReviewer.Domain/          # Entities and domain rules (no dependencies)
│  ├─ MusicReviewer.Infrastructure/  # EF Core DbContext, migrations, external API clients
│  └─ musicreviewer-web/             # Vue 3 + Vite (JavaScript)
├─ tests/
│  ├─ MusicReviewer.UnitTests/
│  └─ MusicReviewer.IntegrationTests/
├─ infra/                            # Bicep (Phase 6)
├─ .github/workflows/                # ci.yml (Phase 0), deploy.yml (Phase 6)
└─ docs/PLAN.md
```

### API surface (v1)
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/health` | Health check (DB connectivity) |
| GET | `/api/search?q=` | Artist search |
| GET | `/api/artists/{id}` | Artist details |
| GET | `/api/artists/{id}/recordings?type=&sort=` | Ranked major recordings |
| GET | `/api/recordings/{id}` | Recording details, tracks, credits and Wikipedia summary |
| GET | `/api/browse/featured`, `/api/genres`, `/api/genres/{slug}` | Browse |

Paging uses `?page=&pageSize=`, and errors use RFC 7807 ProblemDetails.

---

## 6. Liquid Glass design system

The goal is Apple's *Liquid Glass* look: translucent, refractive layers that float over rich content, with the content itself (album art) supplying the colour. We recreate it with our own CSS. **No Apple assets, icons or trademarked fonts are used.**

### Principles
1. **Content first.** Album art and artist imagery form the base layer. Glass is used only for controls and navigation that float above it.
2. **Layered depth.** There are three elevation levels (`--glass-1/2/3`), each with more blur, a stronger edge highlight and a deeper shadow.
3. **Adaptive tint.** A dominant colour is taken from the cover art and drives the page's background gradient and the glass tint.
4. **Fluid motion.** Springy transitions, the View Transitions API with a Vue `<Transition>` fallback, and a floating nav that shrinks on scroll.
5. **Shapes.** Large, continuous corner radii (20–32px), pill-shaped buttons, and a capsule search field.

### Accessibility guardrails (non-negotiable)
- `prefers-reduced-transparency: reduce` switches glass to solid surfaces.
- `prefers-reduced-motion: reduce` turns off springs and morphs.
- Text on glass must meet WCAG AA contrast. A scrim sits behind text whenever the tint is too light.
- Keyboard navigation works everywhere, focus rings are visible, and semantic landmarks are used.

### Components
`GlassPanel`, `GlassCard`, `GlassButton`, `GlassSearchField`, `GlassNavBar`, `GlassSheet`, `GlassChip`, `GlassToast`, `AdaptiveBackdrop`.

---

## 7. Data model (v1)

```
Artist          Id, MusicBrainzId, Name, SortName, Disambiguation, Type, Country,
                BeginYear, EndYear, ImageUrl, LastSyncedUtc, IsFeatured, Wikipedia*
Recording       Id, ArtistId, MusicBrainzId, Title, PrimaryType, SecondaryTypes (flags),
                FirstReleaseDate, FirstReleaseYear, Label, CoverArtUrl,
                NotabilityScore, LastSyncedUtc, Wikipedia*
*WikipediaArticle (owned type, stored as Wikipedia_* columns on its owner):
                PageTitle, PageUrl, RevisionId, Extract, FetchedUtc
Track           Id, RecordingId, Position, DiscNumber, Title, DurationMs
Credit          Id, RecordingId, PersonName, Role, Instrument
Genre           Id, Name, Slug        ArtistGenre / RecordingGenre (join tables)
IngestionJob    Id, Type, TargetId, Status, Attempts, Error, CreatedUtc, CompletedUtc
```
v2 adds: `AppUser`, `Review`, `Follow`, `EmailPreference`, `EmailLog`.

---

## 8. Delivery phases

| Phase | Scope | Exit criteria |
|---|---|---|
| **0 · Scaffold** ✅ | Solution and projects, Vue app, Vite proxy, EF Core + LocalDB, health endpoint, lint/format, GitHub Actions CI | `dotnet test` and `npm run build` pass; the SPA calls `/api/health` |
| **1 · Glass UI shell** ✅ | Design tokens, glass components, layout, routing, placeholder views, light and dark themes | A component showcase page works; reduced-transparency mode works |
| **2 · Catalog** | MusicBrainz, CAA and Wikidata clients; ingestion jobs; search; artist and recording pages | Searching for any well-known artist shows ranked albums with art and tracks |
| **3 · Wikipedia summaries** | Wikipedia client, linking via Wikidata, summary storage and refresh, attribution UI | The top recordings show Wikipedia summaries with attribution |
| **4 · Azure deploy** | Bicep (SQL, App Service, SWA, Key Vault, App Insights), managed identities, `deploy.yml` | Merging to `main` deploys to dev |
| **5 · Polish** | Refraction effects, view transitions, performance budget, accessibility audit | Lighthouse ≥ 90 for performance and accessibility |
| **v2** | Entra External ID, reviews, follows, ACS email, admin | — |

---

## 9. Local development

- **API:** `dotnet run --project src/MusicReviewer.Api`
- **Web:** `npm run dev` in `src/musicreviewer-web`. Vite proxies `/api` to the API.
- **DB:** SQL Server LocalDB (`(localdb)\MSSQLLocalDB`), set up with `dotnet ef database update`. On first run in Development, the API applies migrations automatically.
- **Secrets:** `dotnet user-secrets`.

---

## 10. Decisions

| # | Decision |
|---|---|
| 1 | Summaries come from **Wikipedia** (lead-section extracts, CC BY-SA attribution). No AI generation. |
| 2 | **Classical music is out** of scope for v1. |
| 3 | **User accounts are deferred to v2.** Email is also v2, since it depends on accounts. |
| 4 | Hosting: **Azure Static Web Apps + App Service**. |
| 5 | **.NET 10 LTS** and **Node 24 LTS**. |
| 6 | CI/CD: **GitHub Actions**. |
| 7 | API style: **Minimal APIs** grouped by feature. |
