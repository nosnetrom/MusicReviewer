# MusicReviewer

Search famous musicians, browse their major recordings, and read Wikipedia summaries of each album.

- **Front end:** Vue 3 + Vite (JavaScript), in `src/musicreviewer-web`
- **Back end:** ASP.NET Core on .NET 10, in `src/MusicReviewer.*`
- **Data:** Azure SQL (SQL Server LocalDB locally), via EF Core

See [docs/PLAN.md](docs/PLAN.md) for the architecture and roadmap.

## Prerequisites

- .NET 10 SDK (the version is pinned in `global.json`)
- Node 24 (the version is pinned in `.nvmrc`)
- SQL Server LocalDB (included with Visual Studio), or any SQL Server instance
- Docker, for the integration tests

## Run locally

```powershell
# API on http://localhost:5080. Applies EF migrations to LocalDB on startup (Development only).
dotnet run --project src/MusicReviewer.Api

# Web on http://localhost:5173. Proxies /api to the API.
cd src/musicreviewer-web
npm install
npm run dev
```

To use a database other than LocalDB, override the connection string:

```powershell
dotnet user-secrets --project src/MusicReviewer.Api set "ConnectionStrings:MusicReviewer" "<connection string>"
```

To point the web dev server at an API on another port, set `API_PROXY_TARGET` (for example `http://localhost:5090`) before `npm run dev`.

### Catalog data

- Artists, albums, tracks and personnel are imported from MusicBrainz the first time a page asks for them. The page shows "Importing…" and fills in as the data arrives.
- Every MusicBrainz call goes through a single rate-limited worker, capped at 1 request/sec as MusicBrainz requires. List and search calls return up to 25 items each. The settings are under `MusicBrainz` in `appsettings.json`.
- In Development, the 24 featured artists in `Catalog:FeaturedArtists` are imported in the background on first run, at low priority, so the Home page fills up over a few minutes.

## Test

```powershell
dotnet test --solution MusicReviewer.slnx          # unit tests + integration tests (needs Docker)
cd src/musicreviewer-web; npm run test:unit -- --run
```

Tests use recorded MusicBrainz and Wikidata responses (`tests/MusicReviewer.UnitTests/Fixtures`) and never call the network. A smoke test against the real services runs only on request:

```powershell
dotnet test --project tests/MusicReviewer.IntegrationTests -- --explicit only
```

## Database migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/MusicReviewer.Infrastructure --startup-project src/MusicReviewer.Api --output-dir Persistence/Migrations
```

## Project layout

| Path | Purpose |
|---|---|
| `src/MusicReviewer.Api` | ASP.NET Core host: Minimal API endpoints, DI, OpenAPI, CORS |
| `src/MusicReviewer.Application` | Use cases and interfaces |
| `src/MusicReviewer.Domain` | Entities and domain rules |
| `src/MusicReviewer.Infrastructure` | EF Core, migrations, external API clients |
| `src/musicreviewer-web` | Vue SPA |
| `tests/` | xUnit v3 unit and integration tests |
| `.github/workflows` | GitHub Actions CI |

## Attribution

Recording and artist summaries come from [Wikipedia](https://www.wikipedia.org/) under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). Discography data comes from [MusicBrainz](https://musicbrainz.org/) (CC0), links to Wikipedia from [Wikidata](https://www.wikidata.org/) (CC0), and cover art from the [Cover Art Archive](https://coverartarchive.org/).
