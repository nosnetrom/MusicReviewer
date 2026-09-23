namespace MusicReviewer.Domain.Catalog;

/// <summary>A performer or group, keyed externally by its MusicBrainz ID.</summary>
public class Artist
{
    public Guid Id { get; set; }
    public Guid MusicBrainzId { get; set; }
    public required string Name { get; set; }
    public required string SortName { get; set; }
    public string? Disambiguation { get; set; }
    public ArtistType Type { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, when known.</summary>
    public string? Country { get; set; }

    public int? BeginYear { get; set; }
    public int? EndYear { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? LastSyncedUtc { get; set; }

    public WikipediaArticle? Wikipedia { get; set; }
    public List<Recording> Recordings { get; } = [];
    public List<Genre> Genres { get; } = [];
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
