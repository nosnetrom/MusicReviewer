# Production Deployment to Azure

## Low-cost production baseline

- **App Service:** one Linux Azure App Service on Basic B1, with Always On enabled and autoscale disabled. It serves the API and the Vue frontend from the same origin, and runs the ingestion worker and MusicBrainz gateway. Keep everything in this single instance for now.
- **Database:** Azure SQL Database Basic, in the same region as the App Service. Confirm its storage and performance limits fit the catalog. Serverless is not cheaper for this app: the ingestion worker queries the database every few seconds, so a serverless database never auto-pauses and bills continuously.
- **Database access:** SQL authentication with two dedicated database users: one for the running app with read/write access only, and one for the deployment workflow with schema-change rights. Use the server admin login only for administration. The app's connection string is stored in Azure Key Vault and referenced from an App Service setting.

This avoids paying for separate frontend hosting, worker, cache, message broker, API gateway, and monitoring services at launch. Prices vary by region and Azure offer; validate the current estimate in the Azure Pricing Calculator and create a budget alert.

Do not scale the API above one instance yet. Each process currently owns its own MusicBrainz rate limiter, gateway, and ingestion worker. Multiple replicas could exceed MusicBrainz's per-IP request limit.

## 1. Create Azure resources

Create a resource group in one region, then create:

1. An Azure SQL logical server with SQL authentication enabled, and a Basic database.
2. A Linux App Service plan and Web App on Basic B1, with the .NET 10 runtime stack.

Connect to the database (not `master`) as the server admin and create the two users:

```sql
-- Used by the running app.
CREATE USER [musicreviewer-app] WITH PASSWORD = '<strong password>';
ALTER ROLE db_datareader ADD MEMBER [musicreviewer-app];
ALTER ROLE db_datawriter ADD MEMBER [musicreviewer-app];

-- Used by the deployment workflow to apply migrations.
CREATE USER [musicreviewer-migrator] WITH PASSWORD = '<strong password>';
ALTER ROLE db_ddladmin ADD MEMBER [musicreviewer-migrator];
ALTER ROLE db_datareader ADD MEMBER [musicreviewer-migrator];
ALTER ROLE db_datawriter ADD MEMBER [musicreviewer-migrator];
```

These are contained database users, so their connection strings must name this database. Avoid semicolons in the passwords; they break connection strings.

Use the Azure-provided domain initially; add a custom domain only when needed.

## 2. Configure the App Service

Set these under the Web App's **Settings → Environment variables → App settings** tab:

- `ASPNETCORE_ENVIRONMENT=Production` (also the default when unset).
- `AZURE_SQL_CONNECTIONSTRING` as a Key Vault reference, such as `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<secret-name>/)`. Leave the version off the URI so the app uses the latest version.
- `Catalog__SeedFeaturedArtistsOnStartup`: see step 5.

Put `AZURE_SQL_CONNECTIONSTRING` on the **App settings** tab, not the **Connection strings** tab. App Service renames entries on the Connection strings tab, and the app stops at startup in Production if it can't find `AZURE_SQL_CONNECTIONSTRING`.

Don't set `Cors__AllowedOrigins__*`. The frontend and API share an origin, so CORS isn't needed.

Don't set `ASPNETCORE_FORWARDEDHEADERS_ENABLED`. The app already reads visitor IPs from App Service's forwarded headers, trusting them only from private network ranges, so the per-client rate limiter works without it.

### Database connection string in Key Vault

The Key Vault secret holds the app user's connection string:

```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=musicreviewer-app;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

The Web App's system-assigned managed identity needs the **Key Vault Secrets User** role on the vault (or a **Get** secrets access policy). Under **Environment variables**, the setting should show a green "Key vault Reference" source; selecting it shows the resolution status.

To change the connection string, for example to rotate the password, add a new version of the secret. App Service caches Key Vault values for up to 24 hours, and **a restart alone does not fetch the new version.** Force a fetch, then restart:

```
id=$(az webapp show -g <resource-group> -n <app-name> --query id -o tsv)
az rest --method post --url "https://management.azure.com$id/config/configreferences/appsettings/refresh?api-version=2022-03-01"
az webapp restart -g <resource-group> -n <app-name>
```

Any app setting change made with **Apply** in the portal also triggers a fetch.

Keep `RateLimiting`, `MusicBrainz`, and `Ingestion` settings at their configured defaults initially.

## 3. Configure GitHub

The `main_musicreviewer.yml` workflow signs in to Azure with OpenID Connect, using the `AZUREAPPSERVICE_CLIENTID_*`, `AZUREAPPSERVICE_TENANTID_*`, and `AZUREAPPSERVICE_SUBSCRIPTIONID_*` secrets that the App Service Deployment Center created. Add:

- **Secret** `AZURE_SQL_MIGRATIONS_CONNECTIONSTRING`: the migrator user's connection string, in the same form as above with `User ID=musicreviewer-migrator`.
- **Variable** `AZURE_RESOURCE_GROUP`: the resource group containing the SQL server.
- **Variable** `AZURE_SQL_SERVER_NAME`: the SQL server name, without `.database.windows.net`.

Give the deployment identity (the app registration in `AZUREAPPSERVICE_CLIENTID_*`) the **SQL Server Contributor** role on the SQL server so the workflow can manage firewall rules:

```
az role assignment create --assignee <client-id> --role "SQL Server Contributor" \
  --scope $(az sql server show -g <resource-group> -n <server> --query id -o tsv)
```

Do not set `VITE_API_BASE_URL`. When it is empty, the frontend calls `/api` on its own origin.

Never commit connection strings or credentials.

## 4. Deploy

Every push to `main` (or a manual run) of `main_musicreviewer.yml` does the following:

1. **CI:** runs `ci.yml` (build, tests, lint, format check). Nothing deploys unless it passes. Pull requests run `ci.yml` on their own.
2. **Build:** publishes the API in Release, builds the Vue app and copies `dist` into the API's `wwwroot`, and builds a self-contained EF Core migrations bundle.
3. **Deploy:**
   1. Signs in to Azure.
   2. Adds a temporary SQL firewall rule for the runner's IP, named `github-actions-<run id>-<attempt>`.
   3. Applies pending migrations with the bundle and the migrator connection string. If the new firewall rule isn't active yet (error 40615), it retries every 30 seconds, up to six attempts.
   4. Removes the firewall rule, even if migrations failed or the run was cancelled.
   5. Deploys the app to App Service, only if migrations succeeded.

Only one deployment runs at a time. The app never applies migrations itself in Production; it does so only in Development.

The App Service serves the Vue app for any path that isn't a file or an API route, so client-side routes like `/artists/<id>` load directly. Unknown `/api/...` paths return 404.

Enable Always On so the hosted ingestion worker keeps processing while the site is idle. Keep the App Service at one instance. Retain platform logs only as long as needed to control costs.

## 5. Seed and verify the catalog

The featured-artist seeder is disabled by default, so a new database starts with an empty catalog. MusicBrainz is limited to approximately one request per second, so importing the full featured list can take a long time; for a quicker start, shorten `Catalog:FeaturedArtists` first.

To run the import:

1. In the Web App's **Settings → Environment variables → App settings** tab, select **+ Add**, enter the name `Catalog__SeedFeaturedArtistsOnStartup` and the value `true`, then select **Apply** and confirm. The app restarts and queues the featured artists for import.
2. Watch progress in the **Log stream**, or as artists appear on the home page.
3. When the import is complete, change the value to `false` (or delete the setting) and **Apply** again, so later restarts don't queue the list again.

From the command line, use `az webapp config appsettings set -g <resource-group> -n <app-name> --settings Catalog__SeedFeaturedArtistsOnStartup=true`, and `=false` afterwards.

Changing any app setting restarts the app, and the site returns errors for roughly 2–4 minutes while the new process starts. Change settings when a short outage is acceptable. Deployments restart the app the same way.

Verify that:

- The deployment run's migrations step succeeded, and no `github-actions-*` firewall rules remain on the SQL server.
- The site loads, and refreshing on a deep link such as an artist page still works.
- `/api/health` reports healthy. This only proves the app can connect to SQL, not that the schema is current.
- Search works (database reads), and an uncached artist import completes and its recording details eventually load (database writes).
- Rapid requests from two different clients are rate-limited separately.
- The App Service remains at one instance and MusicBrainz calls remain serialized.

If the app can't reach the database, check the Web App's **Log stream**: "Login failed for user '…'" means a wrong user or password; "permission was denied" means a missing role. If the user named in "Login failed" isn't the one in the current Key Vault secret, the app is still using a cached value; force a fetch as described in step 2.

## Cost controls and later scaling

Prefer one region and the smallest database tier that meets measured needs. Key Vault, which holds the database connection string, costs very little at this volume. Avoid adding Application Insights, Azure API Management, Front Door, Redis, or Service Bus until a specific operational need justifies them. Basic platform logs and Azure budgets are sufficient to start. Moving SQL access to Microsoft Entra managed identity later would remove the stored database passwords.

Before scaling the API to multiple instances, move the ingestion worker and MusicBrainz gateway into a dedicated single-instance process. Keep jobs in the existing SQL-backed queue initially; a separate broker is not required until measurements show the database queue is a bottleneck. Route live MusicBrainz searches through that worker too, then add a shared edge rate limit for all API replicas. Until this change is made, scale the database or the single API instance vertically rather than horizontally.

A small self-managed VM may have a lower bill, but then you own OS patching, TLS, SQL Server Express limits, backups, monitoring, and recovery. Treat that as a cost-versus-operations tradeoff rather than the default production recommendation.
