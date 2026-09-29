using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Catalog;

namespace MusicReviewer.Infrastructure.Ingestion;

/// <summary>
/// Startup catalog upkeep: queues genre classification for any imported artist missing it,
/// then seeds the featured artists when <see cref="CatalogOptions.SeedFeaturedArtistsOnStartup"/> is set.
/// </summary>
public sealed class CatalogStartupService(IServiceScopeFactory scopes, IOptions<CatalogOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var scope = scopes.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<GenreReclassifier>().QueueUnclassifiedAsync(stoppingToken);

        if (!options.Value.SeedFeaturedArtistsOnStartup || options.Value.FeaturedArtists.Count == 0)
            return;

        await using (var scope = scopes.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<FeaturedArtistSeeder>().SeedAsync(stoppingToken);
    }
}
