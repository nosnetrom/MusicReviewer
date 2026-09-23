using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Infrastructure.Persistence.Configurations;

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        // Workers poll for the oldest queued jobs.
        builder.HasIndex(j => new { j.Status, j.CreatedUtc });
        builder.HasIndex(j => new { j.Type, j.TargetId });
        builder.Property(j => j.Error).HasMaxLength(4000);
    }
}
