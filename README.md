# MusicReviewer

Search famous musicians, browse their major recordings, and read Wikipedia summaries of each album.

- **Front end:** Vue 3 + Vite (JavaScript), in `src/musicreviewer-web`
- **Back end:** ASP.NET Core on .NET 10, in `src/MusicReviewer.*`
- **Data:** Azure SQL (SQL Server locally), via EF Core

See [docs/PLAN.md](docs/PLAN.md) for the architecture and roadmap.

Development works on Windows, macOS and Linux. The only difference is the local database: see [Database](#database).

## Quick start

On Windows with Visual Studio installed (it provides LocalDB), no setup beyond the [prerequisites](#prerequisites) is needed:

```bash
dotnet run --project src/MusicReviewer.Api                      # terminal 1: API on :5080
cd src/musicreviewer-web && npm install && npm run dev          # terminal 2: web on :5173
```

Open http://localhost:5173. On macOS or Linux, [start SQL Server in Docker](#macos-or-linux-sql-server-in-docker) first.

## Prerequisites

- .NET 10 SDK (the version is pinned in `global.json`)
- Node 24 (the version is pinned in `.nvmrc`; with nvm or fnm, run `nvm use` in the repo)
- A local SQL Server: LocalDB on Windows, or the Docker container in `compose.yaml` anywhere
- Docker (Docker Desktop, OrbStack or Colima), for the integration tests and for SQL Server on macOS or Linux
- An editor: Visual Studio or VS Code on Windows; VS Code with the C# Dev Kit, or JetBrains Rider, on macOS

## Database

### Windows: LocalDB

Nothing to set up. `appsettings.Development.json` points at `(localdb)\MSSQLLocalDB`, which comes with Visual Studio.

### macOS or Linux: SQL Server in Docker

LocalDB is Windows-only, so run SQL Server 2022 in a container instead:

1. **Apple Silicon only:** the SQL Server image is x86-64 only. In Docker Desktop, turn on *Settings → General → Use Rosetta for x86_64/amd64 emulation*. OrbStack does this automatically.
2. Create a `.env` file in the repo root (it is git-ignored) with a strong password: at least 8 characters, using three of upper case, lower case, digits and symbols.

   ```bash
   echo 'MSSQL_SA_PASSWORD=<password>' > .env
   ```

3. Start the container. `--wait` returns once SQL Server accepts connections.

   ```bash
   docker compose up -d --wait
   ```

4. Point the API at it. User secrets stay outside the repo and override `appsettings.Development.json`:

   ```bash
   dotnet user-secrets --project src/MusicReviewer.Api set "ConnectionStrings:MusicReviewer" \
     "Server=localhost,1433;Database=MusicReviewer;User Id=sa;Password=<password>;TrustServerCertificate=True"
   ```

The data is kept in the `musicreviewer-sql` Docker volume between runs. `docker compose stop` pauses the container; `docker compose down -v` deletes it and its data.

The same user-secrets command points the API at any other SQL Server, such as an Azure SQL database.

### Moving existing data between machines

A new database fills itself: in Development the featured artists are imported on first run (see [Catalog data](#catalog-data)). To copy an existing catalog instead, export a `.bacpac` with [SqlPackage](https://learn.microsoft.com/sql/tools/sqlpackage/sqlpackage-download), which runs on Windows, macOS and Linux:

```bash
# On the old machine (LocalDB shown)
sqlpackage /Action:Export /SourceConnectionString:"Server=(localdb)\MSSQLLocalDB;Database=MusicReviewer;Trusted_Connection=True;TrustServerCertificate=True" /TargetFile:MusicReviewer.bacpac

# On the new machine, into a database that does not exist yet
sqlpackage /Action:Import /SourceFile:MusicReviewer.bacpac /TargetConnectionString:"Server=localhost,1433;Database=MusicReviewer;User Id=sa;Password=<password>;TrustServerCertificate=True"
```

## Run locally

```bash
# API on http://localhost:5080. Applies EF migrations on startup (Development only).
dotnet run --project src/MusicReviewer.Api

# Web on http://localhost:5173, in a second terminal. Proxies /api to the API.
cd src/musicreviewer-web
npm install
npm run dev
```

To point the web dev server at an API on another port, set `API_PROXY_TARGET` (for example `http://localhost:5090`) before `npm run dev`. Deployed builds instead call the API at `VITE_API_BASE_URL` (see `src/musicreviewer-web/.env.example`).

### API

In Development the OpenAPI document is at http://localhost:5080/openapi/v1.json, and `GET /api/health` reports the health checks.

| Endpoint | Returns |
|---|---|
| `GET /api/search?q=` | Artists: local matches plus a live MusicBrainz search |
| `GET /api/search/suggestions` | Imported artist names and genres, used as example searches |
| `GET /api/artists/{mbid}` | Artist details; the first request queues the discography import |
| `GET /api/artists/{mbid}/recordings?type=&sort=` | Recordings. `type`: `studio` (default), `ep`, `live`, `compilation`, `all`. `sort`: `notability` (default), `date` |
| `GET /api/recordings/{mbid}` | Recording details, track list and personnel |
| `GET /api/browse/featured` | Featured artists for Home |
| `GET /api/browse/recordings?genre=&decade=&offset=&limit=` | Studio albums by genre slug and/or decade (`1970s`), paged up to 60 |
| `GET /api/genres` | Genres for browsing |

### Catalog data

- Artists, albums, tracks and personnel are imported from MusicBrainz the first time a page asks for them. The page shows "Importing…" and fills in as the data arrives.
- Every MusicBrainz call goes through a single rate-limited worker, capped at 1 request/sec as MusicBrainz requires. List and search calls return up to 25 items each. The settings are under `MusicBrainz` in `appsettings.json`.
- In Development, the 72 featured artists in `Catalog:FeaturedArtists` are imported in the background on first run, at low priority. At 1 request/sec the Home page fills up over roughly half an hour to an hour.
- Wikipedia summaries (artist bios and album leads) are fetched during imports, and a startup backfill fills in any that are missing or more than 30 days old.

## Test

```bash
dotnet test --solution MusicReviewer.slnx          # unit tests + integration tests (needs Docker running)
cd src/musicreviewer-web && npm run test:unit -- --run
```

Tests use recorded MusicBrainz, Wikidata and Wikipedia responses (`tests/MusicReviewer.UnitTests/Fixtures`) and never call the network. The integration tests start their own throwaway SQL Server container, so they don't need the one in `compose.yaml` (on Apple Silicon they need the same Rosetta setting). A smoke test against the real services runs only on request:

```bash
dotnet test --project tests/MusicReviewer.IntegrationTests -- --explicit only
```

## Lint and format

The web app uses oxlint, ESLint and Prettier. CI runs the check-only versions, so run these before pushing:

```bash
cd src/musicreviewer-web
npm run lint            # oxlint + ESLint, fixing what they can
npm run format          # Prettier
npm run lint:check && npm run format:check   # what CI runs
```

C# style is set in `.editorconfig`, and the .NET analyzers run at `latest-recommended`. Analyzer warnings are errors in CI (where `CI=true`) but not in local builds, so a clean local build can still fail CI. Fix any warnings before pushing.

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push to `main` and every pull request. It builds and tests the .NET solution, and it lints, format-checks, tests and builds the web app.

## Database migrations

`dotnet-ef` is pinned as a local tool in `dotnet-tools.json`:

```bash
dotnet tool restore   # once per clone
dotnet ef migrations add <Name> --project src/MusicReviewer.Infrastructure --startup-project src/MusicReviewer.Api --output-dir Persistence/Migrations
```

The API applies pending migrations on startup in Development.

## Project layout

| Path | Purpose |
|---|---|
| `src/MusicReviewer.Api` | ASP.NET Core host: Minimal API endpoints, DI, OpenAPI, CORS |
| `src/MusicReviewer.Application` | Use cases and interfaces |
| `src/MusicReviewer.Domain` | Entities and domain rules |
| `src/MusicReviewer.Infrastructure` | EF Core, migrations, external API clients |
| `src/musicreviewer-web` | Vue SPA |
| `tests/MusicReviewer.UnitTests` | xUnit v3 unit tests, with recorded API responses in `Fixtures/` |
| `tests/MusicReviewer.IntegrationTests` | API tests against a Testcontainers SQL Server |
| `docs/PLAN.md` | Architecture, roadmap and decisions |
| `.github/workflows` | GitHub Actions CI |
| `compose.yaml` | SQL Server 2022 container for local development on macOS or Linux |

## Attribution

Recording and artist summaries come from [Wikipedia](https://www.wikipedia.org/) under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). Discography data comes from [MusicBrainz](https://musicbrainz.org/) (CC0), links to Wikipedia from [Wikidata](https://www.wikidata.org/) (CC0), and cover art from the [Cover Art Archive](https://coverartarchive.org/).
