using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Catalog;

namespace MusicReviewer.UnitTests.Catalog;

public class CatalogServiceTests
{
    [Theory]
    [InlineData("1970s", 1970)]
    [InlineData("1970", 1970)]
    [InlineData("2020S", 2020)]
    [InlineData("1975", null)]
    [InlineData("seventies", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Parses_decades(string? input, int? expected) =>
        Assert.Equal(expected, CatalogService.ParseDecade(input));

    private static RecordingSummaryDto Album(string artist, string title, double notability) =>
        new(Guid.NewGuid(), title, null, ArtistId(artist), artist, Domain.Catalog.ReleaseType.Album,
            Domain.Catalog.ReleaseCategories.Studio, 1970, null, notability, false);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Guid> ArtistIds = new();

    private static Guid ArtistId(string name) => ArtistIds.GetOrAdd(name, _ => Guid.NewGuid());

    [Fact]
    public void Browse_ranks_each_artists_top_three_before_anyones_fourth()
    {
        // Already in notability order: A dominates the top of the list.
        var byNotability = new[]
        {
            Album("A", "A1", 90), Album("A", "A2", 85), Album("A", "A3", 80), Album("A", "A4", 75), Album("A", "A5", 70),
            Album("B", "B1", 60), Album("C", "C1", 55), Album("B", "B2", 50),
        };

        var ranked = CatalogService.RankForBrowse(byNotability).Select(r => r.Title);

        Assert.Equal(["A1", "A2", "A3", "B1", "C1", "B2", "A4", "A5"], ranked);
    }

    [Fact]
    public void Browse_ranking_keeps_every_recording_exactly_once()
    {
        var byNotability = Enumerable.Range(0, 50)
            .Select(i => Album($"Artist {i % 7}", $"Album {i}", 100 - i))
            .ToList();

        var ranked = CatalogService.RankForBrowse(byNotability);

        Assert.Equal(byNotability.Count, ranked.Count);
        Assert.Equal(byNotability.Select(r => r.Mbid).Order(), ranked.Select(r => r.Mbid).Order());
        // 7 artists × 3 = the first 21 hold no more than three per artist.
        Assert.All(ranked.Take(21).GroupBy(r => r.ArtistMbid), g => Assert.True(g.Count() <= 3));
    }

    [Fact]
    public void Pages_advance_by_items_fetched_even_when_some_are_filtered_out()
    {
        var page = new Page<string>(Total: 60, Offset: 25, Items: ["kept"], Fetched: 25);

        Assert.Equal(50, page.NextOffset);
        Assert.True(page.HasMore);
        Assert.False(new Page<string>(60, 50, [], Fetched: 10).HasMore);
        Assert.False(new Page<string>(60, 50, [], Fetched: 0).HasMore);
    }
}
