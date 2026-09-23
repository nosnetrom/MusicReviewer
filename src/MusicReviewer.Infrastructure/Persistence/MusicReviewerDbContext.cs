using Microsoft.EntityFrameworkCore;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;

namespace MusicReviewer.Infrastructure.Persistence;

public class MusicReviewerDbContext(DbContextOptions<MusicReviewerDbContext> options) : DbContext(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Recording> Recordings => Set<Recording>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MusicReviewerDbContext).Assembly);
}
