using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Abstractions;

// Source-neutral shapes returned by the external clients. Infrastructure maps
// MusicBrainz / Wikidata JSON into these so application code never sees wire formats.

public sealed record ArtistInfo(
    Guid MusicBrainzId,
    string Name,
    string SortName,
    string? Disambiguation,
    ArtistType Type,
    string? Country,
    int? BeginYear,
    int? EndYear,
    string? WikidataId,
    IReadOnlyList<GenreInfo> Genres,
    int? SearchScore = null);

public sealed record GenreInfo(string Name, int Count);

public sealed record ReleaseGroupInfo(
    Guid MusicBrainzId,
    string Title,
    string? ArtistCredit,
    Guid? PrimaryArtistId,
    ReleaseType PrimaryType,
    SecondaryReleaseTypes SecondaryTypes,
    string? FirstReleaseDate);

/// <param name="Total">Total matches reported by the source.</param>
/// <param name="Offset">Offset this page was requested at.</param>
/// <param name="Items">Items kept after any client-side filtering.</param>
/// <param name="Fetched">Items the source actually returned, which drives paging even when some were filtered out.</param>
public sealed record Page<T>(int Total, int Offset, IReadOnlyList<T> Items, int Fetched)
{
    public int NextOffset => Offset + Fetched;

    public bool HasMore => Fetched > 0 && NextOffset < Total;
}

public sealed record ReleaseDetail(
    Guid MusicBrainzId,
    string? Label,
    IReadOnlyList<TrackInfo> Tracks,
    IReadOnlyList<CreditInfo> Credits);

public sealed record TrackInfo(int DiscNumber, int Position, string Title, int? DurationMs);

public sealed record CreditInfo(string PersonName, string Role, string? Instrument);

/// <param name="Title">The article's canonical title, after following redirects.</param>
/// <param name="Extract">Plain text of the lead section; paragraphs separated by newlines.</param>
public sealed record WikipediaLead(string Title, string Url, long RevisionId, string Extract);

public sealed record WikidataLink(string WikidataId, string? EnglishWikipediaTitle, int Sitelinks);
