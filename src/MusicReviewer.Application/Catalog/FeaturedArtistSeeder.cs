using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Catalog;

/// <summary>
/// Ensures the configured featured artists exist and are marked featured, queueing
/// low-priority imports so visitor-triggered imports still run first.
/// </summary>
public sealed partial class FeaturedArtistSeeder(
    IMusicReviewerDbContext db,
    CatalogService catalog,
    IOptions<CatalogOptions> options,
    ILogger<FeaturedArtistSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var mbid in options.Value.FeaturedArtists.Distinct())
        {
            try
            {
                var artist = await db.Artists.FirstOrDefaultAsync(a => a.MusicBrainzId == mbid, cancellationToken)
                    ?? await catalog.CreateArtistAsync(mbid, RequestPriority.Background, cancellationToken);

                if (!artist.IsFeatured)
                {
                    artist.IsFeatured = true;
                    await db.SaveChangesAsync(cancellationToken);
                }

                await catalog.EnsureDiscographyAsync(artist, JobPriority.Background, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogSeedFailed(logger, mbid, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not seed featured artist {Mbid}")]
    private static partial void LogSeedFailed(ILogger logger, Guid mbid, Exception exception);
}
