namespace MusicReviewer.Domain.Ingestion;

/// <summary>A queued unit of background work that pulls data from an external source.</summary>
public class IngestionJob
{
    public Guid Id { get; set; }
    public IngestionJobType Type { get; set; }

    /// <summary>The artist or recording the job applies to.</summary>
    public Guid TargetId { get; set; }

    public IngestionJobStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
}

public enum IngestionJobType
{
    ArtistDiscography = 1,
    WikipediaSummary = 2,
    ArtistRefresh = 3,
}

public enum IngestionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
}
