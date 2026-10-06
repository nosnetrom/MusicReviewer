# musicreviewer-web

The MusicReviewer front end: a Vue 3 single-page app built with Vite, in plain JavaScript (`<script setup>` single-file components, JSDoc for types), with Vue Router and Pinia.

For the full project setup (API, database, tests), see the [root README](../../README.md).

## Develop

The API must be running on http://localhost:5080 (`dotnet run --project src/MusicReviewer.Api` from the repo root).

```bash
npm install
npm run dev       # http://localhost:5173; /api is proxied to the API
```

| Variable | Used by | Purpose |
|---|---|---|
| `API_PROXY_TARGET` | `npm run dev` | Proxies `/api` to an API on another address. Default: `http://localhost:5080` |
| `VITE_API_BASE_URL` | `npm run build` | Base URL of the deployed API. Leave it empty locally. See `.env.example` |

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Dev server with hot reload |
| `npm run build` / `npm run preview` | Production build into `dist/`, and a local preview of it |
| `npm run test:unit` | Vitest in watch mode (add `-- --run` for a single run, as CI does) |
| `npm run lint` | oxlint, then ESLint, fixing what they can |
| `npm run format` | Prettier on `src/` |
| `npm run lint:check` / `npm run format:check` | Check-only versions, run by CI |

## Layout

| Path | Contents |
|---|---|
| `src/api` | API client functions |
| `src/views` | Route-level pages |
| `src/router` | Route table |
| `src/stores` | Pinia stores |
| `src/components` | App-wide pieces: logo, icons, empty and sync states |
| `src/components/glass` | Liquid Glass design-system components (see `docs/PLAN.md` §6) |
| `src/components/music` | Artist, recording and track components |
| `src/composables` | Shared composition functions |
| `src/utils` | Pure helpers |
| `src/styles` | Global CSS and design tokens |
| `public/` | Icons, web manifest and `staticwebapp.config.json` for Azure Static Web Apps |

Tests sit next to the code they cover in `__tests__` folders.

The [Vue (Official)](https://marketplace.visualstudio.com/items?itemName=Vue.volar) extension is recommended for VS Code.
