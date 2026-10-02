using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Catalog;

public sealed record ArtistSummaryDto(
    Guid Mbid,
    string Name,
    string? Disambiguation,
    ArtistType Type,
    string? Country,
    int? BeginYear,
    int? EndYear,
    bool IsImported);

/// <summary>An imported artist's name and broad genre slugs, for search-field suggestions.</summary>
public sealed record ArtistSuggestionDto(string Name, IReadOnlyList<string> Genres);

public sealed record SearchResultDto(string Query, IReadOnlyList<ArtistSummaryDto> Artists, bool RemoteAvailable);

public sealed record GenreDto(string Name, string Slug);

/// <summary>
/// A Wikipedia lead section, shown as a summary. CC BY-SA 4.0: always display it with
/// attribution and a link to <see cref="Url"/>.
/// </summary>
public sealed record WikipediaSummaryDto(string Title, string Url, long RevisionId, DateTime FetchedUtc, IReadOnlyList<string> Paragraphs)
{
    public static WikipediaSummaryDto? From(WikipediaArticle? article) =>
        article is null ? null : new(article.PageTitle, article.PageUrl, article.RevisionId, article.FetchedUtc, article.Paragraphs);
}

public sealed record ArtistDetailDto(
    Guid Mbid,
    string Name,
    string SortName,
    string? Disambiguation,
    ArtistType Type,
    string? Country,
    int? BeginYear,
    int? EndYear,
    string? WikipediaTitle,
    IReadOnlyList<GenreDto> Genres,
    IReadOnlyList<string> Styles,
    SyncStatus SyncStatus,
    int RecordingCount,
    WikipediaSummaryDto? Summary);

public sealed record RecordingSummaryDto(
    Guid Mbid,
    string Title,
    string? ArtistCredit,
    Guid ArtistMbid,
    string ArtistName,
    ReleaseType PrimaryType,
    ReleaseCategories Category,
    int? Year,
    string? CoverArtUrl,
    double Notability,
    bool HasWikipediaArticle);

public sealed record ArtistRecordingsDto(
    RecordingFilter Filter,
    SyncStatus SyncStatus,
    IReadOnlyList<RecordingSummaryDto> Recordings,
    int Offset,
    int Total,
    bool HasMore);

public sealed record TrackDto(int Disc, int Position, string Title, int? DurationMs);

public sealed record CreditDto(string Name, string? Instruments);

public sealed record CreditGroupDto(string Role, IReadOnlyList<CreditDto> People);

public sealed record RecordingDetailDto(
    Guid Mbid,
    string Title,
    string? ArtistCredit,
    Guid ArtistMbid,
    string ArtistName,
    ReleaseType PrimaryType,
    ReleaseCategories Category,
    string? FirstReleaseDate,
    int? Year,
    string? Label,
    string? CoverArtUrl,
    string? WikipediaTitle,
    SyncStatus DetailsStatus,
    IReadOnlyList<TrackDto> Tracks,
    IReadOnlyList<CreditGroupDto> Credits,
    WikipediaSummaryDto? Summary);

public sealed record FeaturedDto(IReadOnlyList<ArtistSummaryDto> Artists, IReadOnlyList<RecordingSummaryDto> Recordings);

public sealed record GenreListItemDto(string Name, string Slug, int ArtistCount);

/// <param name="Offset">Position of the first recording in the full ranked list.</param>
/// <param name="Total">Recordings matching the filters, across all pages.</param>
public sealed record BrowseRecordingsDto(
    string? Genre,
    string? Decade,
    IReadOnlyList<RecordingSummaryDto> Recordings,
    int Offset,
    int Total,
    bool HasMore);

/// <summary>Which of an artist's recordings to list.</summary>
public enum RecordingFilter
{
    /// <summary>Studio albums (the default view).</summary>
    Studio,

    EP,
    Live,
    Compilation,

    /// <summary>Everything imported so far.</summary>
    All,
}

public enum RecordingSort
{
    Notability,
    Date,
}

public sealed class NotFoundException(string message) : Exception(message);
