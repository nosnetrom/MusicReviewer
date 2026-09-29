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
