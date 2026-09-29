using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.UnitTests.Catalog;

public class WikipediaSummariesTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeWikipedia(Func<string, WikipediaLead?> lookup) : IWikipediaClient
    {
        public List<int> BatchSizes { get; } = [];
        public bool FailNextCall { get; set; }

        public Task<IReadOnlyDictionary<string, WikipediaLead>> GetLeadsAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken)
        {
            BatchSizes.Add(titles.Count);
            if (FailNextCall)
            {
                FailNextCall = false;
                throw new ExternalServiceUnavailableException("Wikipedia");
            }

            IReadOnlyDictionary<string, WikipediaLead> leads = titles
                .Select(t => (t, lead: lookup(t)))
                .Where(x => x.lead is not null)
                .ToDictionary(x => x.t, x => x.lead!);
            return Task.FromResult(leads);
        }
    }

    private static WikipediaLead Lead(string title, long revision, string extract = "Lead paragraph.") =>
        new(title, $"https://en.wikipedia.org/wiki/{title.Replace(' ', '_')}", revision, extract);

    private static Recording Album(string title, string? wikipediaTitle) =>
        new() { Title = title, WikipediaTitle = wikipediaTitle };

    private static WikipediaSummaries Create(FakeWikipedia client) =>
        new(client, new FakeTimeProvider(Now), NullLogger<WikipediaSummaries>.Instance);

    [Fact]
    public async Task Stores_the_lead_as_the_summary()
    {
        var album = Album("Kind of Blue", "Kind of Blue");
        var summaries = Create(new FakeWikipedia(t => Lead(t, 100, "First.\nSecond.")));

        var changed = await summaries.RefreshAsync([album], TestContext.Current.CancellationToken);

        Assert.Equal(1, changed);
        Assert.NotNull(album.Wikipedia);
        Assert.Equal("https://en.wikipedia.org/wiki/Kind_of_Blue", album.Wikipedia.PageUrl);
        Assert.Equal(100, album.Wikipedia.RevisionId);
        Assert.Equal(["First.", "Second."], album.Wikipedia.Paragraphs);
        Assert.Equal(Now.UtcDateTime, album.Wikipedia.FetchedUtc);
    }

    [Fact]
    public async Task Keeps_the_stored_text_when_the_revision_is_unchanged()
    {
        var album = Album("Kind of Blue", "Kind of Blue");
        album.Wikipedia = new WikipediaArticle
        {
            PageTitle = "Kind of Blue",
            PageUrl = "https://en.wikipedia.org/wiki/Kind_of_Blue",
            RevisionId = 100,
            Extract = "Stored text.",
            FetchedUtc = Now.UtcDateTime.AddDays(-40),
        };
        var summaries = Create(new FakeWikipedia(t => Lead(t, 100, "Fetched text.")));

        var changed = await summaries.RefreshAsync([album], TestContext.Current.CancellationToken);

        Assert.Equal(0, changed);
        Assert.Equal("Stored text.", album.Wikipedia.Extract);
        Assert.Equal(Now.UtcDateTime, album.Wikipedia.FetchedUtc); // checked, so no longer stale
    }

    [Fact]
    public async Task Replaces_the_text_when_the_article_has_a_new_revision()
    {
        var album = Album("Kind of Blue", "Kind of Blue");
        album.Wikipedia = new WikipediaArticle { PageTitle = "Kind of Blue", PageUrl = "u", RevisionId = 100, Extract = "Old." };
        var summaries = Create(new FakeWikipedia(t => Lead(t, 101, "New.")));

        await summaries.RefreshAsync([album], TestContext.Current.CancellationToken);

        Assert.Equal(101, album.Wikipedia.RevisionId);
        Assert.Equal("New.", album.Wikipedia.Extract);
    }

    [Fact]
    public async Task Fetches_in_batches_of_twenty_and_only_for_items_with_an_article()
    {
        var albums = Enumerable.Range(1, 45).Select(i => Album($"Album {i}", $"Album {i}")).ToList();
        albums.Add(Album("No article", null));
        var client = new FakeWikipedia(t => Lead(t, 1));

        await Create(client).RefreshAsync(albums, TestContext.Current.CancellationToken);

        Assert.Equal([20, 20, 5], client.BatchSizes);
        Assert.All(albums.Take(45), a => Assert.NotNull(a.Wikipedia));
        Assert.Null(albums[^1].Wikipedia);
    }

    [Fact]
    public async Task Skips_articles_that_no_longer_exist_and_carries_on_after_a_failed_batch()
    {
        var albums = Enumerable.Range(1, 25).Select(i => Album($"Album {i}", $"Album {i}")).ToList();
        var client = new FakeWikipedia(t => t == "Album 22" ? null : Lead(t, 1)) { FailNextCall = true };

        await Create(client).RefreshAsync(albums, TestContext.Current.CancellationToken);

        Assert.All(albums.Take(20), a => Assert.Null(a.Wikipedia)); // first batch failed
        Assert.Null(albums[21].Wikipedia); // "Album 22": missing on Wikipedia
        Assert.Equal(4, albums.Skip(20).Count(a => a.Wikipedia is not null));
    }
}
