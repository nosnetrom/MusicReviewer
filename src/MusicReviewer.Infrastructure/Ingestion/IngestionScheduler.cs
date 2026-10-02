using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Ingestion;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.Infrastructure.Ingestion;

/// <summary>Wakes the <see cref="IngestionWorker"/> as soon as a job is queued.</summary>
public sealed class IngestionSignal : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Notify()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already signalled; the worker will look for all pending jobs when it wakes.
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _semaphore.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _semaphore.Dispose();
}

public sealed class IngestionScheduler(
    MusicReviewerDbContext db,
    IngestionSignal signal,
    TimeProvider clock,
    IOptions<IngestionOptions> options) : IIngestionScheduler
{
    public async Task EnqueueAsync(
        IngestionJobType type,
        Guid targetId,
        string parameter = "",
        JobPriority priority = JobPriority.UserRequested,
        CancellationToken cancellationToken = default)
    {
        var active = db.IngestionJobs.Where(j => j.Type == type && j.TargetId == targetId && j.Parameter == parameter
            && (j.Status == IngestionJobStatus.Queued || j.Status == IngestionJobStatus.Running));

        if (await active.AnyAsync(cancellationToken))
        {
            // A visitor is now waiting on a queued background job: move it up.
            await active
                .Where(j => j.Status == IngestionJobStatus.Queued && j.Priority > priority)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.Priority, priority), cancellationToken);
            signal.Notify();
            return;
        }

        var queued = await db.IngestionJobs.CountAsync(j => j.Status == IngestionJobStatus.Queued, cancellationToken);
        if (queued >= options.Value.MaxQueuedJobs)
            throw new ExternalServiceUnavailableException("Catalog");

        var now = clock.GetUtcNow().UtcDateTime;
        var job = new IngestionJob
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            TargetId = targetId,
            Parameter = parameter,
            Priority = priority,
            Status = IngestionJobStatus.Queued,
            CreatedUtc = now,
            NotBeforeUtc = now,
        };
        db.IngestionJobs.Add(job);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race with an identical enqueue (the unique index kept only one); anything else is real.
            db.Entry(job).State = EntityState.Detached;
            if (!await active.AnyAsync(cancellationToken))
                throw;
        }

        signal.Notify();
    }
}
