using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Application.Abstractions;

public interface IMusicReviewerDbContext
{
    DbSet<Artist> Artists { get; }
    DbSet<Recording> Recordings { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Credit> Credits { get; }
    DbSet<Genre> Genres { get; }
    DbSet<IngestionJob> IngestionJobs { get; }

    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
