using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Ingestion;

/// <summary>
/// Imports an artist's release groups for one category, page by page, saving after each page
/// so the artist page fills in progressively. Enrichment from Wikidata and the Cover Art Archive
/// is best-effort: a failure there leaves the recording imported without it.
/// </summary>
public sealed partial class ArtistImporter(
    IMusicReviewerDbContext db,
    IMusicBrainzClient musicBrainz,
    IWikidataClient wikidata,
    ICoverArtClient coverArt,
    TimeProvider clock,
    ILogger<ArtistImporter> logger)
{
    private const int CoverArtParallelism = 4;

    public async Task ImportCategoryAsync(Artist artist, ReleaseCategories category, CancellationToken cancellationToken)
    {
        var offset = 0;
        Page<ReleaseGroupInfo> page;
        do
        {
            page = await musicBrainz.SearchReleaseGroupsAsync(artist.MusicBrainzId, category, offset, cancellationToken);
            var recordings = await UpsertAsync(artist, page.Items, cancellationToken);
            await EnrichAsync(recordings, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            offset = page.NextOffset;
        }
        while (page.HasMore);

        artist.ImportedCategories |= category;
    }

    public async Task ApplyArtistWikidataAsync(Artist artist, CancellationToken cancellationToken)
    {
        try
        {
            var links = await wikidata.GetArtistLinksAsync([artist.MusicBrainzId], cancellationToken);
            if (links.TryGetValue(artist.MusicBrainzId, out var link))
            {
                artist.WikidataId = link.WikidataId;
                artist.WikipediaTitle = link.EnglishWikipediaTitle;
                artist.WikidataSitelinks = link.Sitelinks;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogEnrichmentFailed(logger, "Wikidata", artist.Name, ex);
        }
    }

    private async Task<List<Recording>> UpsertAsync(Artist artist, IReadOnlyList<ReleaseGroupInfo> items, CancellationToken cancellationToken)
    {
        var ids = items.Select(i => i.MusicBrainzId).ToList();
        var existing = await db.Recordings
            .Where(r => ids.Contains(r.MusicBrainzId))
            .ToDictionaryAsync(r => r.MusicBrainzId, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        var result = new List<Recording>(items.Count);

        foreach (var item in items.DistinctBy(i => i.MusicBrainzId))
        {
            if (!existing.TryGetValue(item.MusicBrainzId, out var recording))
            {
                recording = new Recording { Id = Guid.CreateVersion7(), ArtistId = artist.Id, MusicBrainzId = item.MusicBrainzId, Title = item.Title };
                db.Recordings.Add(recording);
            }

            // A recording belongs to whichever artist imported it first; later imports only refresh its facts.
            recording.Title = item.Title;
            recording.ArtistCredit = item.ArtistCredit;
            recording.PrimaryType = item.PrimaryType;
            recording.SecondaryTypes = item.SecondaryTypes;
            recording.FirstReleaseDate = item.FirstReleaseDate;
            recording.FirstReleaseYear = PartialDate.Year(item.FirstReleaseDate);
            recording.LastSyncedUtc = now;
            result.Add(recording);
        }

        return result;
    }

    private async Task EnrichAsync(List<Recording> recordings, CancellationToken cancellationToken)
    {
        if (recordings.Count == 0)
            return;

        try
        {
            var links = await wikidata.GetReleaseGroupLinksAsync([.. recordings.Select(r => r.MusicBrainzId)], cancellationToken);
            foreach (var recording in recordings)
            {
                if (links.TryGetValue(recording.MusicBrainzId, out var link))
                {
                    recording.WikidataId = link.WikidataId;
                    recording.WikipediaTitle = link.EnglishWikipediaTitle;
                    recording.WikidataSitelinks = link.Sitelinks;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogEnrichmentFailed(logger, "Wikidata", $"{recordings.Count} recordings", ex);
        }

        var needCovers = recordings.Where(r => r.CoverArtUrl is null).ToList();
        var covers = new Dictionary<Guid, string>();
        await Parallel.ForEachAsync(
            needCovers,
            new ParallelOptions { MaxDegreeOfParallelism = CoverArtParallelism, CancellationToken = cancellationToken },
            async (recording, ct) =>
            {
                try
                {
                    var url = await coverArt.GetFrontCoverUrlAsync(recording.MusicBrainzId, ct);
                    if (url is not null)
                        lock (covers) covers[recording.Id] = url;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogEnrichmentFailed(logger, "Cover Art Archive", recording.Title, ex);
                }
            });

        foreach (var recording in recordings)
        {
            if (covers.TryGetValue(recording.Id, out var url))
                recording.CoverArtUrl = url;
            recording.RecalculateNotability();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source} enrichment failed for {Target}; continuing without it")]
    private static partial void LogEnrichmentFailed(ILogger logger, string source, string target, Exception exception);
}
