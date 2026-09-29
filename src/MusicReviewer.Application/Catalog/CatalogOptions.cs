namespace MusicReviewer.Application.Catalog;

public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>Imported data older than this is refreshed in the background when viewed.</summary>
    public TimeSpan RefreshAfter { get; set; } = TimeSpan.FromDays(30);

    /// <summary>How long live MusicBrainz search results are cached.</summary>
    public TimeSpan SearchCacheDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How long search waits for MusicBrainz before returning local results only.</summary>
    public TimeSpan RemoteSearchTimeout { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>Import these artists (MusicBrainz IDs) at startup and mark them featured.</summary>
    public List<Guid> FeaturedArtists { get; set; } = [];

    public bool SeedFeaturedArtistsOnStartup { get; set; }
}
