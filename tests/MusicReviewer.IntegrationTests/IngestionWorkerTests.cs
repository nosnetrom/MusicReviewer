using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicReviewer.Domain.Ingestion;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.IntegrationTests;

public class IngestionWorkerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Leaves_job_types_it_cannot_handle_in_the_queue()
    {
        var ct = TestContext.Current.CancellationToken;
        _ = factory.CreateClient(); // start the host and its worker

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        var now = DateTime.UtcNow;
        var job = new IngestionJob
        {
            Id = Guid.CreateVersion7(),
            Type = (IngestionJobType)999, // a type from a newer build
            TargetId = Guid.NewGuid(),
            Status = IngestionJobStatus.Queued,
            CreatedUtc = now,
            NotBeforeUtc = now,
        };
        db.IngestionJobs.Add(job);
        await db.SaveChangesAsync(ct);

        // Give the worker (polling every 200ms in tests) several chances to pick it up.
        await Task.Delay(1500, ct);

        var status = await db.IngestionJobs.AsNoTracking().Where(j => j.Id == job.Id).Select(j => j.Status).SingleAsync(ct);
        Assert.Equal(IngestionJobStatus.Queued, status);
    }
}
