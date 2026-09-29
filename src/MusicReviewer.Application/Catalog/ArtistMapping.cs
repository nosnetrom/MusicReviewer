using Microsoft.EntityFrameworkCore;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Catalog;

internal static class ArtistMapping
{
    private const int MaxStyles = 5;

    public static Artist Create(ArtistInfo info)
    {
        var artist = new Artist { Id = Guid.CreateVersion7(), MusicBrainzId = info.MusicBrainzId, SortName = info.SortName };
        Apply(artist, info);
        return artist;
    }

    public static void Apply(Artist artist, ArtistInfo info)
    {
        artist.Name = info.Name;
        artist.SortName = info.SortName;
        artist.Disambiguation = string.IsNullOrWhiteSpace(info.Disambiguation) ? null : info.Disambiguation;
        artist.Type = info.Type;
        artist.Country = info.Country;
        artist.BeginYear = info.BeginYear;
        artist.EndYear = info.EndYear;
        artist.WikidataId = info.WikidataId ?? artist.WikidataId;
    }

    /// <summary>
    /// Sets the artist's broad genres (for browsing) from the weight of their MusicBrainz genre
    /// votes, and keeps their most-voted specific genres as <see cref="Artist.Styles"/>.
    /// </summary>
    public static async Task ApplyGenresAsync(IMusicReviewerDbContext db, Artist artist, IReadOnlyList<GenreInfo> genres, CancellationToken cancellationToken)
    {
        var voted = genres.Where(g => g.Count > 0).OrderByDescending(g => g.Count).ToList();
        var families = GenreFamilies.Classify(voted.Select(g => (g.Name, g.Count)));

        var slugs = families.Select(f => f.Slug).ToList();
        var existing = await db.Genres.Where(g => slugs.Contains(g.Slug)).ToListAsync(cancellationToken);
        existing.AddRange(db.Genres.Local.Where(g => slugs.Contains(g.Slug) && !existing.Contains(g)));

        artist.Genres.Clear();
        foreach (var family in families)
        {
            var genre = existing.FirstOrDefault(g => g.Slug == family.Slug);
            if (genre is null)
            {
                genre = new Genre { Id = Guid.CreateVersion7(), Name = family.Name, Slug = family.Slug };
                db.Genres.Add(genre);
                existing.Add(genre);
            }

            artist.Genres.Add(genre);
        }

        // Empty (not null) marks the artist as classified even when MusicBrainz has no genres for them.
        artist.Styles = string.Join(", ", voted.Take(MaxStyles).Select(g => g.Name));
    }
}
