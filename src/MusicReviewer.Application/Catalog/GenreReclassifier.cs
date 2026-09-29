using Microsoft.EntityFrameworkCore;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Catalog;

/// <summary>
/// Brings imported artists' broad genres up to the current <see cref="GenreFamilies.Version"/>.
/// Artists with stored votes are re-classified immediately; older ones (no stored votes) get a
/// low-priority job that fetches their votes from MusicBrainz once.
/// </summary>
public sealed class GenreReclassifier(IMusicReviewerDbContext db, IIngestionScheduler scheduler)
{
    public sealed record Result(int Reclassified, int Queued);

    public async Task<Result> RunAsync(CancellationToken cancellationToken)
    {
        var outdated = await db.Artists
            .Include(a => a.Genres)
            .Where(a => a.LastSyncedUtc != null && (a.Styles == null || a.GenresVersion < GenreFamilies.Version))
            .ToListAsync(cancellationToken);

        var needVotes = new List<Guid>();
        var reclassified = 0;
        foreach (var artist in outdated)
        {
            if (ArtistMapping.StoredVotes(artist) is { } votes)
            {
                await ArtistMapping.ApplyGenresAsync(db, artist, votes, cancellationToken);
                reclassified++;
            }
            else
            {
                needVotes.Add(artist.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var id in needVotes)
            await scheduler.EnqueueAsync(IngestionJobType.ArtistGenres, id, priority: JobPriority.Background, cancellationToken: cancellationToken);

        return new Result(reclassified, needVotes.Count);
    }
}
