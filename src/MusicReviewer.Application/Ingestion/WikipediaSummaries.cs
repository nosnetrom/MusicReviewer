using Microsoft.Extensions.Logging;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Ingestion;

/// <summary>
/// Fetches and stores Wikipedia lead sections as summaries for artists and recordings.
/// Requests are batched (<see cref="IWikipediaClient.MaxTitlesPerRequest"/> titles at a time), and an
/// article whose revision hasn't changed keeps its stored text. Best-effort: a failed batch is
/// logged and skipped so the rest of an import still completes.
/// </summary>
public sealed partial class WikipediaSummaries(IWikipediaClient wikipedia, TimeProvider clock, ILogger<WikipediaSummaries> logger)
{
    /// <returns>How many items now have a summary that was added or updated.</returns>
    public async Task<int> RefreshAsync(IEnumerable<IHasWikipediaArticle> items, CancellationToken cancellationToken)
    {
        var byTitle = items
            .Where(i => !string.IsNullOrWhiteSpace(i.WikipediaTitle))
            .GroupBy(i => i.WikipediaTitle!)
            .ToList();

        var changed = 0;
        foreach (var batch in byTitle.Chunk(IWikipediaClient.MaxTitlesPerRequest))
        {
            IReadOnlyDictionary<string, WikipediaLead> leads;
            try
            {
                leads = await wikipedia.GetLeadsAsync([.. batch.Select(g => g.Key)], cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogBatchFailed(logger, batch.Length, ex);
                continue;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            foreach (var group in batch)
            {
                if (!leads.TryGetValue(group.Key, out var lead))
                    continue;

                foreach (var item in group)
                {
                    if (item.Wikipedia is { } current && current.RevisionId == lead.RevisionId)
                    {
                        current.FetchedUtc = now; // unchanged: just note that we checked
                        continue;
                    }

                    item.Wikipedia = new WikipediaArticle
                    {
                        PageTitle = lead.Title,
                        PageUrl = lead.Url,
                        RevisionId = lead.RevisionId,
                        Extract = lead.Extract,
                        FetchedUtc = now,
                    };
                    changed++;
                }
            }
        }

        return changed;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wikipedia request for {Count} articles failed; continuing without them")]
    private static partial void LogBatchFailed(ILogger logger, int count, Exception exception);
}
