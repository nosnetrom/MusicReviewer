using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Ingestion;

public interface IIngestionScheduler
{
    /// <summary>
    /// Queues a job unless an identical one is already queued or running.
    /// Saves immediately so the worker can pick it up.
    /// </summary>
    Task EnqueueAsync(
        IngestionJobType type,
        Guid targetId,
        string parameter = "",
        JobPriority priority = JobPriority.UserRequested,
        CancellationToken cancellationToken = default);
}

/// <summary>Runs one type of ingestion job.</summary>
public interface IIngestionJobHandler
{
    IngestionJobType Type { get; }

    Task HandleAsync(IngestionJob job, CancellationToken cancellationToken);

    /// <summary>Called once a job has used all its attempts, to record the failure on the target.</summary>
    Task OnFailedAsync(IngestionJob job, CancellationToken cancellationToken);
}
