namespace MusicReviewer.Infrastructure.MusicBrainz;

public sealed class MusicBrainzOptions
{
    public const string SectionName = "MusicBrainz";

    public Uri BaseUrl { get; set; } = new("https://musicbrainz.org/ws/2/");

    /// <summary>Required by MusicBrainz: application name, version and a contact URL or email.</summary>
    public string UserAgent { get; set; } = "MusicReviewer/0.1 ( https://github.com/nosnetrom/MusicReviewer )";

    /// <summary>Maximum items per list or search request.</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>Minimum spacing between requests. MusicBrainz allows 1 request/sec on average per IP.</summary>
    public TimeSpan MinInterval { get; set; } = TimeSpan.FromMilliseconds(1050);

    /// <summary>Extra pause for all traffic after MusicBrainz answers 503 (rate limited or overloaded).</summary>
    public TimeSpan PauseAfterUnavailable { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Timeout for a single HTTP attempt; MusicBrainz can be slow under load.</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
