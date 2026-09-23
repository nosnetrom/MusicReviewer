using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Infrastructure.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasIndex(a => a.MusicBrainzId).IsUnique();
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.IsFeatured).HasFilter("[IsFeatured] = 1");

        builder.Property(a => a.Name).HasMaxLength(400);
        builder.Property(a => a.SortName).HasMaxLength(400);
        builder.Property(a => a.Disambiguation).HasMaxLength(400);
        builder.Property(a => a.Country).HasMaxLength(2).IsFixedLength();
        builder.Property(a => a.ImageUrl).HasMaxLength(2048);

        builder.OwnsOne(a => a.Wikipedia, WikipediaArticleMapping.Configure);

        builder.HasMany(a => a.Genres).WithMany(g => g.Artists).UsingEntity("ArtistGenre");
    }
}

internal sealed class RecordingConfiguration : IEntityTypeConfiguration<Recording>
{
    public void Configure(EntityTypeBuilder<Recording> builder)
    {
        builder.HasIndex(r => r.MusicBrainzId).IsUnique();
        builder.HasIndex(r => new { r.ArtistId, r.NotabilityScore }).IsDescending(false, true);
        builder.HasIndex(r => r.Title);

        builder.Property(r => r.Title).HasMaxLength(500);
        builder.Property(r => r.FirstReleaseDate).HasMaxLength(10);
        builder.Property(r => r.Label).HasMaxLength(400);
        builder.Property(r => r.CoverArtUrl).HasMaxLength(2048);

        builder.HasOne(r => r.Artist).WithMany(a => a.Recordings).HasForeignKey(r => r.ArtistId);
        builder.HasMany(r => r.Tracks).WithOne().HasForeignKey(t => t.RecordingId);
        builder.HasMany(r => r.Credits).WithOne().HasForeignKey(c => c.RecordingId);
        builder.HasMany(r => r.Genres).WithMany(g => g.Recordings).UsingEntity("RecordingGenre");

        builder.OwnsOne(r => r.Wikipedia, WikipediaArticleMapping.Configure);

        builder.Ignore(r => r.IsStudioAlbum);
    }
}

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasIndex(t => new { t.RecordingId, t.DiscNumber, t.Position });
        builder.Property(t => t.Title).HasMaxLength(500);
    }
}

internal sealed class CreditConfiguration : IEntityTypeConfiguration<Credit>
{
    public void Configure(EntityTypeBuilder<Credit> builder)
    {
        builder.Property(c => c.PersonName).HasMaxLength(400);
        builder.Property(c => c.Role).HasMaxLength(200);
        builder.Property(c => c.Instrument).HasMaxLength(200);
    }
}

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasIndex(g => g.Slug).IsUnique();
        builder.Property(g => g.Name).HasMaxLength(100);
        builder.Property(g => g.Slug).HasMaxLength(100);
    }
}

internal static class WikipediaArticleMapping
{
    /// <summary>Stores the article inline on the owner's table as Wikipedia_* columns.</summary>
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, WikipediaArticle> wiki)
        where TOwner : class
    {
        wiki.Property(w => w.PageTitle).HasMaxLength(400);
        wiki.Property(w => w.PageUrl).HasMaxLength(2048);
    }
}
