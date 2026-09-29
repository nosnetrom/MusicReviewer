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

    /// <summary>Plain text of the article's lead section; paragraphs separated by newlines.</summary>
    public required string Extract { get; set; }

    /// <summary>When the article was last fetched or confirmed unchanged.</summary>
    public DateTime FetchedUtc { get; set; }

    public IReadOnlyList<string> Paragraphs =>
        [.. Extract.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}

/// <summary>An entity that can carry a Wikipedia summary: an artist or a recording.</summary>
public interface IHasWikipediaArticle
{
    /// <summary>The English Wikipedia article title, found via Wikidata.</summary>
    string? WikipediaTitle { get; }

    WikipediaArticle? Wikipedia { get; set; }
}
