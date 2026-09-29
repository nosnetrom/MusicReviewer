using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Catalog;

/// <summary>
/// Queues low-priority Wikipedia summary jobs at startup for imported artists whose bio, or any of
/// whose recordings' summaries, is missing or older than <see cref="CatalogOptions.RefreshAfter"/>.
/// Covers data imported before summaries existed, and keeps summaries in step with Wikipedia.
/// </summary>
public sealed class WikipediaBackfill(
    IMusicReviewerDbContext db,
    IIngestionScheduler scheduler,
    IOptions<CatalogOptions> options,
    TimeProvider clock)
{
    /// <returns>How many artists were queued.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime - options.Value.RefreshAfter;

        var artistIds = await db.Artists
            .Where(a => a.LastSyncedUtc != null
                && ((a.WikipediaTitle != null && (a.Wikipedia == null || a.Wikipedia.FetchedUtc < cutoff))
                    || a.Recordings.Any(r => r.WikipediaTitle != null && (r.Wikipedia == null || r.Wikipedia.FetchedUtc < cutoff))))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in artistIds)
            await scheduler.EnqueueAsync(IngestionJobType.WikipediaSummary, id, priority: JobPriority.Background, cancellationToken: cancellationToken);

        return artistIds.Count;
    }
}
