using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Ingestion;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.Infrastructure.Ingestion;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>How often to look for due jobs when not woken by a new one.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Delay before the first retry; each later retry waits 4× longer.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Runs queued <see cref="IngestionJob"/>s one at a time, highest priority and oldest first.
/// Jobs that throw are retried with backoff up to <see cref="IngestionJob.MaxAttempts"/> times.
/// </summary>
public sealed partial class IngestionWorker(
    IServiceScopeFactory scopes,
    IngestionSignal signal,
    TimeProvider clock,
    IOptions<IngestionOptions> options,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var ranJob = false;
            try
            {
                ranJob = await RunNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWorkerError(logger, ex);
            }

            if (!ranJob)
            {
                try
                {
                    await signal.WaitAsync(options.Value.PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <summary>Returns jobs left "Running" by a previous process to the queue.</summary>
    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
            await db.IngestionJobs
                .Where(j => j.Status == IngestionJobStatus.Running)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, IngestionJobStatus.Queued), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogWorkerError(logger, ex);
        }
    }

    /// <returns>true if a job was claimed (whether or not it succeeded).</returns>
    internal async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        var now = clock.GetUtcNow().UtcDateTime;

        // Only claim job types this build can run. During a rolling deploy an older instance
        // shares the queue with a newer one and must leave the newer job types alone.
        var handlers = scope.ServiceProvider.GetServices<IIngestionJobHandler>().ToList();
        var handledTypes = handlers.Select(h => h.Type).ToList();

        var jobId = await db.IngestionJobs
            .Where(j => j.Status == IngestionJobStatus.Queued && j.NotBeforeUtc <= now && handledTypes.Contains(j.Type))
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedUtc)
            .Select(j => j.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (jobId == Guid.Empty)
            return false;

        // Claim atomically so a second worker (Phase 4) can never run the same job.
        var claimed = await db.IngestionJobs
            .Where(j => j.Id == jobId && j.Status == IngestionJobStatus.Queued)
            .ExecuteUpdateAsync(
                s => s.SetProperty(j => j.Status, IngestionJobStatus.Running).SetProperty(j => j.Attempts, j => j.Attempts + 1),
                cancellationToken);
        if (claimed == 0)
            return true;

        var job = await db.IngestionJobs.AsNoTracking().FirstAsync(j => j.Id == jobId, cancellationToken);
        var handler = handlers.First(h => h.Type == job.Type);

        try
        {
            LogJobStarted(logger, job.Type, job.TargetId, job.Parameter, job.Attempts);
            await handler.HandleAsync(job, cancellationToken);
            await FinishAsync(db, job.Id, IngestionJobStatus.Succeeded, null, cancellationToken);
            LogJobSucceeded(logger, job.Type, job.TargetId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down: give the attempt back and let the next process run it.
            await db.IngestionJobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(
                s => s.SetProperty(j => j.Status, IngestionJobStatus.Queued).SetProperty(j => j.Attempts, j => j.Attempts - 1),
                CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            var error = Truncate($"{ex.GetType().Name}: {ex.Message}", 4000);

            if (job.Attempts >= IngestionJob.MaxAttempts)
            {
                LogJobFailed(logger, job.Type, job.TargetId, job.Attempts, ex);
                await FinishAsync(db, job.Id, IngestionJobStatus.Failed, error, cancellationToken);
                try
                {
                    await handler.OnFailedAsync(job, cancellationToken);
                }
                catch (Exception onFailed) when (onFailed is not OperationCanceledException)
                {
                    LogWorkerError(logger, onFailed);
                }
            }
            else
            {
                var delay = options.Value.RetryDelay * Math.Pow(4, job.Attempts - 1);
                LogJobRetrying(logger, job.Type, job.TargetId, job.Attempts, delay, ex);
                var notBefore = clock.GetUtcNow().UtcDateTime + delay;
                await db.IngestionJobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(
                    s => s.SetProperty(j => j.Status, IngestionJobStatus.Queued)
                        .SetProperty(j => j.Error, error)
                        .SetProperty(j => j.NotBeforeUtc, notBefore),
                    cancellationToken);
            }
        }

        return true;
    }

    private Task<int> FinishAsync(MusicReviewerDbContext db, Guid jobId, IngestionJobStatus status, string? error, CancellationToken cancellationToken)
    {
        var completed = clock.GetUtcNow().UtcDateTime;
        return db.IngestionJobs.Where(j => j.Id == jobId).ExecuteUpdateAsync(
            s => s.SetProperty(j => j.Status, status).SetProperty(j => j.Error, error).SetProperty(j => j.CompletedUtc, completed),
            cancellationToken);
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting {Type} for {TargetId} {Parameter} (attempt {Attempt})")]
    private static partial void LogJobStarted(ILogger logger, IngestionJobType type, Guid targetId, string parameter, int attempt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished {Type} for {TargetId}")]
    private static partial void LogJobSucceeded(ILogger logger, IngestionJobType type, Guid targetId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Type} for {TargetId} failed on attempt {Attempt}; retrying in {Delay}")]
    private static partial void LogJobRetrying(ILogger logger, IngestionJobType type, Guid targetId, int attempt, TimeSpan delay, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Type} for {TargetId} failed after {Attempts} attempts")]
    private static partial void LogJobFailed(ILogger logger, IngestionJobType type, Guid targetId, int attempts, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion worker error")]
    private static partial void LogWorkerError(ILogger logger, Exception exception);
}
