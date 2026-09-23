namespace MusicReviewer.Domain.Catalog;

/// <summary>
/// An album-level work (a MusicBrainz release group), independent of any particular pressing or edition.
/// </summary>
public class Recording
{
    public Guid Id { get; set; }
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public Guid MusicBrainzId { get; set; }
    public required string Title { get; set; }
    public ReleaseType PrimaryType { get; set; }
    public SecondaryReleaseTypes SecondaryTypes { get; set; }

    /// <summary>MusicBrainz partial date: "1959", "1959-08" or "1959-08-17".</summary>
    public string? FirstReleaseDate { get; set; }

    public int? FirstReleaseYear { get; set; }
    public string? Label { get; set; }
    public string? CoverArtUrl { get; set; }

    /// <summary>Ranking signal used to order an artist's major recordings; higher is more notable.</summary>
    public double NotabilityScore { get; set; }

    public DateTime? LastSyncedUtc { get; set; }

    public WikipediaArticle? Wikipedia { get; set; }
    public List<Track> Tracks { get; } = [];
    public List<Credit> Credits { get; } = [];
    public List<Genre> Genres { get; } = [];

    /// <summary>A plain studio album: shown in an artist's default "major recordings" view.</summary>
    public bool IsStudioAlbum => PrimaryType == ReleaseType.Album && SecondaryTypes == SecondaryReleaseTypes.None;
}

public enum ReleaseType
{
    Other = 0,
    Album = 1,
#pragma warning disable CA1720 // MusicBrainz's own term for this release type.
    Single = 2,
#pragma warning restore CA1720
    EP = 3,
    Broadcast = 4,
}

[Flags]
public enum SecondaryReleaseTypes
{
    None = 0,
    Compilation = 1 << 0,
    Soundtrack = 1 << 1,
    Spokenword = 1 << 2,
    Interview = 1 << 3,
    Audiobook = 1 << 4,
    AudioDrama = 1 << 5,
    Live = 1 << 6,
    Remix = 1 << 7,
    DjMix = 1 << 8,
    Mixtape = 1 << 9,
    Demo = 1 << 10,
    FieldRecording = 1 << 11,
}
