# Production Deployment to Azure

## Low-cost production baseline

- **Frontend:** Azure Static Web Apps Free plan.
- **API and background workers:** one Linux Azure App Service on Basic B1, with Always On enabled and autoscale disabled. Keep the API, ingestion worker, and MusicBrainz gateway in this single instance for now.
- **Database:** Azure SQL Database Basic, in the same region as the App Service. Confirm its storage and performance limits fit the catalog. Compare with serverless SQL if usage is intermittent and cold starts are acceptable.
- **Identity:** use the App Service managed identity for SQL access. Use a separate deployment identity with schema-change permissions for migrations.

This avoids paying for separate worker, cache, message broker, API gateway, and monitoring services at launch. Prices vary by region and Azure offer; validate the current estimate in the Azure Pricing Calculator and create a budget alert.

Do not scale the API above one instance yet. Each process currently owns its own MusicBrainz rate limiter, gateway, and ingestion worker. Multiple replicas could exceed MusicBrainz's per-IP request limit.

## 1. Create Azure resources

Create a resource group in one region, then create:

1. An Azure SQL logical server and a Basic database named `MusicReviewer`.
2. A Linux App Service plan and Web App on Basic B1.
3. An Azure Static Web App on the Free plan.

Enable the Web App's system-assigned managed identity. Configure the SQL server's Microsoft Entra administrator, create a database user for the Web App identity, and grant only the permissions the running API needs. Give the deployment identity separate permissions to apply migrations.

Use the Azure-provided domains initially; add a custom domain only when needed.

## 2. Configure the API

Set these App Service application settings:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__MusicReviewer` to the Azure SQL connection string using Microsoft Entra managed identity authentication.
- `Cors__AllowedOrigins__0` to the exact Static Web App origin, such as `https://<app>.azurestaticapps.net`.
- `Catalog__SeedFeaturedArtistsOnStartup=false` after the initial catalog import.

Keep `RateLimiting`, `MusicBrainz`, and `Ingestion` settings at their configured defaults initially. Confirm forwarded client IP handling behind App Service's proxy so the per-client API limiter does not treat every visitor as the same IP. Trust forwarded headers only from the hosting proxy.

The API applies EF migrations only in Development. Apply production migrations in the deployment workflow before releasing the API, using an idempotent migration script or migration bundle and the deployment identity. Do not enable automatic production migrations at application startup.

## 3. Deploy the frontend

Set `VITE_API_BASE_URL` to the public App Service URL when building the Vue application. The variable is embedded in the frontend bundle at build time. Deploy `src/musicreviewer-web` with `npm ci` and `npm run build`, using `dist` as the output directory.

Configure the Static Web App's GitHub integration or a GitHub Actions deployment workflow. The API's CORS origin must match the deployed frontend origin exactly.

## 4. Deploy the API

Add a GitHub Actions deployment workflow alongside the existing CI workflow. Use GitHub Actions OpenID Connect federation to authenticate to Azure, publish the API in Release configuration, apply database migrations, then deploy the published output to the Web App. Avoid committing publish profiles, connection strings, or credentials.

Enable Always On so the ASP.NET Core hosted ingestion worker continues processing while the site is idle. Keep the App Service at one instance. Use the health endpoint for a basic availability check and retain platform logs only as long as needed to control costs.

## 5. Seed and verify the catalog

The featured-artist seeder is disabled by default. For the initial catalog, use a small curated featured list and enable `Catalog:SeedFeaturedArtistsOnStartup` for a controlled import. MusicBrainz is limited to approximately one request per second, so importing the full featured list can take a long time. Disable seeding after the initial import is complete.

Verify that:

- The Static Web App loads and its API URL points to the production Web App.
- `/api/health` reports healthy and the API can connect to Azure SQL.
- CORS permits requests from the Static Web App and rejects unconfigured origins.
- Search works, an uncached artist import completes, and recording details eventually load.
- The App Service remains at one instance and MusicBrainz calls remain serialized.

## Cost controls and later scaling

Prefer one region and the smallest database tier that meets measured needs. Azure SQL Basic is predictable; serverless may cost less for intermittent use but can add resume latency and still incurs storage costs. Avoid adding Key Vault, Application Insights, Azure API Management, Front Door, Redis, or Service Bus until a specific operational need justifies them. Managed identity avoids a SQL password secret; basic platform logs and Azure budgets are sufficient to start.

Before scaling the API to multiple instances, move the ingestion worker and MusicBrainz gateway into a dedicated single-instance process. Keep jobs in the existing SQL-backed queue initially; a separate broker is not required until measurements show the database queue is a bottleneck. Route live MusicBrainz searches through that worker too, then add a shared edge rate limit for all API replicas. Until this change is made, scale the database or the single API instance vertically rather than horizontally.

A small self-managed VM may have a lower bill, but then you own OS patching, TLS, SQL Server Express limits, backups, monitoring, and recovery. Treat that as a cost-versus-operations tradeoff rather than the default production recommendation.
