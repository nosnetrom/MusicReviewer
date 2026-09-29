namespace MusicReviewer.Domain.Catalog;

/// <summary>A performer or group, keyed externally by its MusicBrainz ID.</summary>
public class Artist : IHasWikipediaArticle
{
    public Guid Id { get; set; }
    public Guid MusicBrainzId { get; set; }

    public string Name
    {
        get;
        set
        {
            field = value;
            NormalizedName = NameNormalizer.Normalize(value);
        }
    } = "";

    /// <summary>Lowercase, accent-free form of <see cref="Name"/> used for local search.</summary>
    public string NormalizedName { get; private set; } = "";

    public required string SortName { get; set; }
    public string? Disambiguation { get; set; }
    public ArtistType Type { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, when known.</summary>
    public string? Country { get; set; }

    public int? BeginYear { get; set; }
    public int? EndYear { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }

    public string? WikidataId { get; set; }
    public string? WikipediaTitle { get; set; }
    public int? WikidataSitelinks { get; set; }

    /// <summary>
    /// The artist's most-voted specific MusicBrainz genres, comma separated (e.g. "hard bop, modal jazz").
    /// Null until genres have been classified; <see cref="Genres"/> holds the broad genres.
    /// </summary>
    public string? Styles { get; set; }

    /// <summary>MusicBrainz genre votes as JSON, kept so genres can be re-classified without refetching.</summary>
    public string? GenreVotes { get; set; }

    /// <summary>The <see cref="GenreFamilies.Version"/> the current <see cref="Genres"/> were classified with.</summary>
    public int GenresVersion { get; set; }

    /// <summary>State of the discography import (studio albums and EPs).</summary>
    public SyncStatus SyncStatus { get; set; }

    /// <summary>Release categories whose recordings have been imported.</summary>
    public ReleaseCategories ImportedCategories { get; set; }

    /// <summary>When the discography was last imported successfully.</summary>
    public DateTime? LastSyncedUtc { get; set; }

    public WikipediaArticle? Wikipedia { get; set; }
    public List<Recording> Recordings { get; } = [];
    public List<Genre> Genres { get; } = [];

    public bool IsStale(DateTime utcNow, TimeSpan maxAge) =>
        LastSyncedUtc is null || utcNow - LastSyncedUtc.Value > maxAge;
}

public enum ArtistType
{
    Other = 0,
    Person = 1,
    Group = 2,
    Orchestra = 3,
    Choir = 4,
    Character = 5,
}
