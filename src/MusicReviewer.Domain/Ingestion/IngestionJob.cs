namespace MusicReviewer.Domain.Ingestion;

/// <summary>A queued unit of background work that pulls data from an external source.</summary>
public class IngestionJob
{
    public const int MaxAttempts = 3;

    public Guid Id { get; set; }
    public IngestionJobType Type { get; set; }

    /// <summary>The artist or recording the job applies to.</summary>
    public Guid TargetId { get; set; }

    /// <summary>Job-specific qualifier, e.g. the release category to import. Empty when unused.</summary>
    public string Parameter { get; set; } = "";

    public IngestionJobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedUtc { get; set; }

    /// <summary>Earliest time the job may run; pushed back after a failed attempt.</summary>
    public DateTime NotBeforeUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }
}

public enum IngestionJobType
{
    /// <summary>Artist details plus studio albums and EPs.</summary>
    ArtistDiscography = 1,

    WikipediaSummary = 2,

    /// <summary>Live albums or compilations for an artist; <see cref="IngestionJob.Parameter"/> names the category.</summary>
    ArtistReleaseCategory = 3,

    /// <summary>Track list, label and personnel for one recording.</summary>
    RecordingDetails = 4,

    /// <summary>Re-classify an artist's genres from a fresh MusicBrainz lookup.</summary>
    ArtistGenres = 5,
}

/// <summary>Lower values run first.</summary>
public enum JobPriority
{
    /// <summary>A visitor is looking at the page that needs this data.</summary>
    UserRequested = 0,

    /// <summary>Seeding and scheduled refreshes.</summary>
    Background = 1,
}

public enum IngestionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
}
