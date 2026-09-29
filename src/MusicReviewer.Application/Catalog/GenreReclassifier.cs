using Microsoft.EntityFrameworkCore;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Catalog;

/// <summary>
/// Queues genre classification for imported artists that don't have it yet — for example after
/// the switch from MusicBrainz's specific genres to broad ones. Runs at low priority.
/// </summary>
public sealed class GenreReclassifier(IMusicReviewerDbContext db, IIngestionScheduler scheduler)
{
    /// <returns>How many artists were queued.</returns>
    public async Task<int> QueueUnclassifiedAsync(CancellationToken cancellationToken)
    {
        var ids = await db.Artists
            .Where(a => a.Styles == null && a.LastSyncedUtc != null)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
            await scheduler.EnqueueAsync(IngestionJobType.ArtistGenres, id, priority: JobPriority.Background, cancellationToken: cancellationToken);

        return ids.Count;
    }
}
