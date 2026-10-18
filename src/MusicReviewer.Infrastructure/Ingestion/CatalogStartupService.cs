using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Catalog;

namespace MusicReviewer.Infrastructure.Ingestion;

/// <summary>
/// Startup catalog upkeep: brings imported artists' genres up to the current classification rules,
/// then seeds the featured artists when <see cref="CatalogOptions.SeedFeaturedArtistsOnStartup"/> is set.
/// </summary>
// public sealed class CatalogStartupService(IServiceScopeFactory scopes, IOptions<CatalogOptions> options) : BackgroundService
// {
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         await using (var scope = scopes.CreateAsyncScope())
//             await scope.ServiceProvider.GetRequiredService<GenreReclassifier>().RunAsync(stoppingToken);

//         await using (var scope = scopes.CreateAsyncScope())
//             await scope.ServiceProvider.GetRequiredService<WikipediaBackfill>().RunAsync(stoppingToken);

//         if (!options.Value.SeedFeaturedArtistsOnStartup || options.Value.FeaturedArtists.Count == 0)
//             return;

//         await using (var scope = scopes.CreateAsyncScope())
//             await scope.ServiceProvider.GetRequiredService<FeaturedArtistSeeder>().SeedAsync(stoppingToken);
//     }
// }

public sealed class CatalogStartupService(
    IServiceScopeFactory scopes,
    IOptions<CatalogOptions> options,
    ILogger<CatalogStartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunStepAsync<GenreReclassifier>((s, ct) => s.RunAsync(ct), stoppingToken);
        await RunStepAsync<WikipediaBackfill>((s, ct) => s.RunAsync(ct), stoppingToken);

        if (!options.Value.SeedFeaturedArtistsOnStartup || options.Value.FeaturedArtists.Count == 0)
            return;

        await RunStepAsync<FeaturedArtistSeeder>((s, ct) => s.SeedAsync(ct), stoppingToken);
    }

    private async Task RunStepAsync<T>(Func<T, CancellationToken, Task> step, CancellationToken ct)
        where T : notnull
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await step(scope.ServiceProvider.GetRequiredService<T>(), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down; nothing to log.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Catalog startup step {Step} failed; continuing.", typeof(T).Name);
        }
    }
}