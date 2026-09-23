namespace MusicReviewer.Domain.Catalog;

/// <summary>
/// The lead-section extract of a Wikipedia article, used as the summary for an artist or recording.
/// Content is CC BY-SA 4.0, so <see cref="PageUrl"/> must always be shown alongside <see cref="Extract"/>.
/// </summary>
public class WikipediaArticle
{
    public required string PageTitle { get; set; }
    public required string PageUrl { get; set; }
    public long RevisionId { get; set; }
    public required string Extract { get; set; }
    public DateTime FetchedUtc { get; set; }
}
