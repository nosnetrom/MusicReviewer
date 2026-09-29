# MusicReviewer — Application Plan

> Status: **v0.4**. Decisions are recorded in [§10](#10-decisions). **Phases 0–3 (scaffold, Glass UI shell, catalog, Wikipedia summaries) are complete**; Phase 4 (Azure deploy) is next.

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
- Coverage across Wikipedias (the Wikidata sitelink count).
- Manual pin or hide (admin, v2).

### Ingestion flow
1. **All MusicBrainz calls go through one worker** that enforces the 1 request/sec limit. Exceeding it makes MusicBrainz reject *every* request from our IP until the rate drops. Search, artist imports and recording imports all queue their calls to this worker. See [Phase 2](#phase-2--catalog-detailed-plan) for details.
2. Search combines local DB matches with a live MusicBrainz search (cached for 10 minutes).
3. When an artist page is opened for the first time, a background job imports the discography:
   - studio albums and EPs, **up to 25 per MusicBrainz request**, paged until the whole list is in
   - Wikidata links and English Wikipedia titles
   - cover-art URLs
   - notability scores
4. When a recording page is opened for the first time, a background job imports its track list and personnel.
5. Data older than 30 days is shown as-is and refreshed in the background.

---

## 4. Summaries (Wikipedia)

- Artists and recordings are linked to Wikipedia through their MusicBrainz ID, then the Wikidata item, then the `enwiki` sitelink. **There is no title-search fallback**: a guessed article is too often the wrong album.
- The summary text is the article's **lead section** as plain text, from the MediaWiki Action API (`action=query&prop=extracts&exintro&explaintext`), up to 20 articles per request. Redirects are followed; missing and disambiguation pages are skipped.
- We store the extract, the article URL, the revision ID and the fetched-at time. Summaries older than 30 days are re-checked; the text is replaced only when the revision has changed.
- The UI shows the first paragraph (or two, when the first is short) with **Read more** for the rest of the lead, then: *"Excerpt from Wikipedia's article "X", available under CC BY-SA 4.0"*, linking to the article and the licence.
- When a recording has no article, the page shows its MusicBrainz facts only, with a "No Wikipedia article is linked" note. When an article is linked but has no usable lead, the page links to it instead.

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
| GET | `/api/artists/{mbid}` | Artist details, genres and sync status |
| GET | `/api/artists/{mbid}/recordings?type=studio\|live\|compilation\|ep\|all&sort=notability\|date` | Ranked major recordings |
| GET | `/api/recordings/{mbid}` | Recording details, tracks, credits, sync status and Wikipedia summary |
| GET | `/api/browse/featured`, `/api/genres`, `/api/browse/recordings?genre=&decade=` | Browse imported data |

Routes use **MusicBrainz IDs** (MBIDs), which are stable and shareable. Pages also work for artists that haven't been imported yet. Paging uses `?page=&pageSize=`. Errors use RFC 7807 ProblemDetails: 404 when MusicBrainz reports the item doesn't exist, 503 with `Retry-After` when MusicBrainz is unavailable.

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
Phase 2 adds:
- `WikidataId` and `WikipediaTitle` on both Artist and Recording.
- `NormalizedName` on Artist: lowercase with accents removed, indexed for prefix search. LocalDB has no full-text search.
- `SyncStatus` on Artist and Recording.
- `DetailsSyncedUtc` and `WikidataSitelinks` on Recording; `WikidataSitelinks` on Artist.
- A unique filtered index on `IngestionJob (Type, TargetId)` covering Queued and Running jobs, so the same import can't be queued twice.

v2 adds: `AppUser`, `Review`, `Follow`, `EmailPreference`, `EmailLog`.

---

## 8. Delivery phases

| Phase | Scope | Exit criteria |
|---|---|---|
| **0 · Scaffold** ✅ | Solution and projects, Vue app, Vite proxy, EF Core + LocalDB, health endpoint, lint/format, GitHub Actions CI | `dotnet test` and `npm run build` pass; the SPA calls `/api/health` |
| **1 · Glass UI shell** ✅ | Design tokens, glass components, layout, routing, placeholder views, light and dark themes | A component showcase page works; reduced-transparency mode works |
| **2 · Catalog** ✅ | MusicBrainz, CAA and Wikidata clients; **single MusicBrainz worker**; ingestion jobs; search; artist and recording pages; seed artists (see below) | Searching for any well-known artist shows ranked albums with art and tracks; CI passes with no external calls |
| **3 · Wikipedia summaries** ✅ | Wikipedia client, linking via Wikidata, summary storage and refresh, attribution UI | The top recordings show Wikipedia summaries with attribution |
| **4 · Azure deploy** | Bicep (SQL, App Service, SWA, Key Vault, App Insights), managed identities, `deploy.yml`; **move the MusicBrainz worker into its own single-instance process** (see below) | Merging to `main` deploys to dev; the API can scale out while MusicBrainz traffic stays at 1 req/sec |
| **5 · Polish** | Refraction effects, view transitions, performance budget, accessibility audit | Lighthouse ≥ 90 for performance and accessibility |
| **v2** | Entra External ID, reviews, follows, ACS email, admin | — |

### Phase 2 · Catalog (detailed plan)

**MusicBrainz calls per import**

| When | Calls |
|---|---|
| Search while typing: a live MusicBrainz search (cached 10 min), combined with local matches | 0–1 |
| First visit to an artist: 1 artist lookup (`inc=url-rels+genres`), plus a **release-group search** for studio albums and EPs at **`limit=25`**, paged | 1 + ⌈studio albums & EPs ÷ 25⌉ |
| Selecting the Live or Compilations filter: the same search for that type, run on demand | ⌈matches ÷ 25⌉ |
| First visit to a recording: pick a representative release, then fetch its tracks, label and personnel | 2 |

Why search rather than browse:
- Browsing with `type=album|ep` can't exclude secondary types. Miles Davis has **685** albums and EPs that way, mostly compilations and live albums; that's 28 calls at 25 per page.
- The search query `arid:<mbid> AND (primarytype:album OR primarytype:ep) AND NOT secondarytype:*` returns just his **93** studio albums and EPs. That's 4 calls, about 5 seconds.

Other services called during an artist import (not subject to the MusicBrainz limit):
- **Wikidata SPARQL**, once per batch of up to 50 MBIDs (`P434` for artists, `P436` for release groups). It returns each item's Wikidata ID, English Wikipedia article and **sitelink count**, meaning how many Wikipedias cover it.
- **Cover Art Archive** to check that each album's front cover exists (a `HEAD` request that answers 307 when one does).

**Notability score:**
- Studio album gets a bonus.
- Having its own English Wikipedia article gets a bonus.
- The Wikidata sitelink count is added on a log scale. It replaces MusicBrainz ratings, which search results don't include and which most albums have only 0–4 votes for.

**Single MusicBrainz worker**
- *Phase 2 (in-process):*
  - All MusicBrainz traffic goes through `MusicBrainzGateway`: one queue with one consumer that sends at most 1 request/sec.
  - Search requests wait on it with a timeout. Import jobs run on the ingestion worker, which also uses the gateway.
  - Retries with backoff on 503 and timeouts.
  - The required User-Agent: `MusicReviewer/0.1 ( https://github.com/nosnetrom/MusicReviewer )`.
- *Phase 4 (separate process):*
  - Host the gateway and ingestion worker as their own single-instance process: a WebJob, or a Container Apps job capped at 1 replica.
  - The API instances then only read and write the database. They queue jobs in the `IngestionJob` table, or in Azure Service Bus if needed.
  - Live search sends a request to the worker and waits for a reply, with a timeout; on timeout it returns local results only.
  - This keeps our outbound MusicBrainz rate at 1/sec however many API instances run.

**Backend tasks**
1. **Migration and domain changes** (the Phase 2 data model additions above).
2. **Clients:** typed clients for MusicBrainz, Wikidata and the Cover Art Archive; JSON mapping for MusicBrainz's hyphenated field names; settings for base URLs, the User-Agent and `PageSize = 25`.
3. **The gateway and ingestion worker:**
   - The worker is a `BackgroundService` over `IngestionJob`, woken by an in-memory signal.
   - Up to 3 attempts with backoff.
   - The unique index stops the same job being queued twice.
4. **Import services:**
   - Search, artist import and recording detail import.
   - Notability scoring: studio album, own Wikipedia article, Wikidata sitelink count. The weights are adjustable and unit-tested.
   - Choosing a representative release: the earliest official release.
5. **Endpoints** from §5 (API surface), with 404 and 503 error responses.
6. **Seed artists:** a configurable list of about 24 well-known artists across genres, imported on first run in development. They become the featured set.

**Frontend tasks**
- **API modules:** for search, artists and recordings, plus a composable that checks back every 2 seconds while an import is running.
- **Search page:** results update as you type (300ms pause) and on Enter, with loading, empty and error states.
- **Artist page:** header and genre chips; Studio, Live, Compilations and EP filters; sort toggle; album grid with real cover art; an "Importing…" state.
- **Recording page:**
  - Cover art, which also tints the page background.
  - A track list that handles multiple discs.
  - Personnel grouped by role.
- **Home and Browse:** use real featured and imported data. Remove `previewRecordings.js`.
- **Footer:** add "Cover art from the Cover Art Archive" attribution.

**Testing**
- **Unit:** mapping, using saved sample responses; notability scoring; representative release choice; partial dates; name normalization; the gateway's rate limit (with a fake clock).
- **Integration:** fake MusicBrainz and Wikidata clients plus the SQL Server container. The scenario is search, open the artist, the job runs, then recordings are returned. Also duplicate jobs and a 404.
- **Live smoke test:** calls the real MusicBrainz; runs only when requested, skipped in CI.
- **Web:** the track list, the search pause and the check-back composable.

**Phase 2 follow-ups**
- **Cover art is slow to appear** (4–9s). The Cover Art Archive redirects to archive.org, which responds slowly. Caching thumbnails in Azure Blob Storage behind a CDN during import would make covers appear immediately. This fits with Phase 4 (Azure).
- **Stale data refreshes when viewed.** There is no nightly refresh job yet.

### Phase 3 · Wikipedia summaries

**When summaries are fetched** (none of this uses the MusicBrainz limit)
- **Artist bio:** during the artist import, right after the Wikidata link is found, so it appears before the albums finish.
- **Studio albums:** in one batch after the studio import; Live and Compilations after their on-demand imports.
- **Any recording:** again when its details are imported, before the page is marked ready.
- **Backfill:** at startup, a low-priority `WikipediaSummary` job is queued for each imported artist whose bio or album summaries are missing or older than 30 days.

**Backend**
- `IWikipediaClient` / `WikipediaClient`: batched Action API calls with the repo User-Agent and the standard resilience handler.
- `WikipediaSummaries`: groups items by article title, fetches in chunks of 20, compares revisions, and logs and skips a failed batch.
- Stored in the existing `Wikipedia_*` columns on Artists and Recordings, so no migration is needed.
- `summary` (title, URL, revision, fetched time, paragraphs) is added to the artist and recording detail responses.

**Frontend**
- `WikipediaSummary.vue`: lead paragraphs, a Read more / Show less toggle (`aria-expanded`), and CC BY-SA attribution.
- Artist page: the bio sits in the header, replacing the plain Wikipedia link.
- Recording page: the About panel shows the summary, a "Fetching…" note while the import runs, or the fallbacks above.

**Testing**
- **Unit:** mapping a recorded Action API response (redirects, normalisation, missing and disambiguation pages); refresh rules (batching, unchanged revisions, failed batches).
- **Integration:** a fake Wikipedia client; summaries appear on the artist and recording endpoints.
- **Live smoke test:** also fetches the Kind of Blue lead.
- **Web:** the paragraph cut-off, the toggle and the attribution.
---

## 9. Local development

- **API:** `dotnet run --project src/MusicReviewer.Api`
- **Web:** `npm run dev` in `src/musicreviewer-web`. Vite proxies `/api` to the API.
- **DB:** SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) on Windows; on macOS or Linux, the SQL Server 2022 container in `compose.yaml`, with the connection string in user secrets (see the README). On first run in Development, the API applies migrations automatically.
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
| 8 | MusicBrainz User-Agent contact is the **repo URL**, not a personal email. |
| 9 | Routes use **MusicBrainz IDs**. |
| 10 | **Albums and EPs only**, no singles. Studio albums are the default view. |
| 11 | Import progress: the page **checks back every 2 seconds** rather than having the server push updates. |
| 12 | Claude drafts the **~24 seed artists**; the list stays editable in config. |
| 13 | MusicBrainz list and search calls return **up to 25 items per request** (`MusicBrainz:PageSize`). This was changed from 10. |
| 14 | **All MusicBrainz calls go through one worker**: in-process in Phase 2, a separate single-instance process in Phase 4. |
| 15 | Browsing uses **12 broad genres** (Jazz, Blues, Rock, Pop, Soul & R&B, Funk & Disco, Hip-Hop, Country, Folk, Electronic, Reggae & Ska, Gospel). Each MusicBrainz tag maps onto them, and an artist gets up to 3 based on their share of the votes. The specific tags are kept as the artist's **styles**. |
| 16 | Summaries show the **first paragraph or two**; **Read more** expands the rest of the lead. |
| 17 | **Artist bios** come from Wikipedia too, as well as recording summaries. |
| 18 | **No artist photos** for now. |
| 19 | **No title-search fallback**: only articles linked through Wikidata are used. |
| 20 | Summaries are **plain text** only (no HTML from Wikipedia). |
