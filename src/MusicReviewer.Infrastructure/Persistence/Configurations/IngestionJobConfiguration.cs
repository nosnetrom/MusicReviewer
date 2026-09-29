using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Infrastructure.Persistence.Configurations;

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        // The worker takes the highest-priority, oldest due job.
        builder.HasIndex(j => new { j.Status, j.Priority, j.NotBeforeUtc, j.CreatedUtc });

        // At most one queued-or-running job per (type, target, parameter).
        builder.HasIndex(j => new { j.Type, j.TargetId, j.Parameter })
            .IsUnique()
            .HasFilter("[Status] IN (0, 1)")
            .HasDatabaseName("IX_IngestionJobs_Active");

        builder.Property(j => j.Parameter).HasMaxLength(50);
        builder.Property(j => j.Error).HasMaxLength(4000);
    }
}
