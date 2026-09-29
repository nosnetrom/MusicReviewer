using MusicReviewer.Infrastructure.Wikidata;

namespace MusicReviewer.UnitTests.External;

public class WikidataMapperTests
{
    [Fact]
    public void Maps_release_group_links_with_english_titles_and_sitelinks()
    {
        var links = WikidataMapper.ToLinks(Fixture.Json<SparqlResponse>("wikidata-sparql-release-groups.json"))
            .ToDictionary(l => l.Mbid, l => l.Link);

        var blueMoods = links[Guid.Parse("39402cf4-888f-3af0-9d0c-0dc7e80baa35")];
        Assert.Equal("Q885776", blueMoods.WikidataId);
        Assert.Equal("Blue Moods", blueMoods.EnglishWikipediaTitle);
        Assert.Equal(6, blueMoods.Sitelinks);

        var youngMan = links[Guid.Parse("093a3235-7271-46d5-9c94-c092edf1fb25")];
        Assert.Equal("Young Man with a Horn (Miles Davis album)", youngMan.EnglishWikipediaTitle);

        // Queried but not on Wikidata.
        Assert.False(links.ContainsKey(Guid.Parse("095878f4-58ac-43f4-93ae-92a1e6588d8d")));
    }

    [Fact]
    public void Maps_artist_link()
    {
        var (mbid, link) = Assert.Single(WikidataMapper.ToLinks(Fixture.Json<SparqlResponse>("wikidata-sparql-artist.json")));

        Assert.Equal(Fixture.MilesDavis, mbid);
        Assert.Equal("Q93341", link.WikidataId);
        Assert.Equal("Miles Davis", link.EnglishWikipediaTitle);
        Assert.True(link.Sitelinks > 50);
    }

    [Theory]
    [InlineData("https://en.wikipedia.org/wiki/Kind_of_Blue", "Kind of Blue")]
    [InlineData("https://en.wikipedia.org/wiki/What%27s_Going_On_(Marvin_Gaye_album)", "What's Going On (Marvin Gaye album)")]
    [InlineData("https://de.wikipedia.org/wiki/Kind_of_Blue", null)]
    public void Extracts_article_titles(string url, string? expected) =>
        Assert.Equal(expected, WikidataMapper.TitleFromArticleUrl(url));

    [Fact]
    public void Query_lists_every_id_for_the_given_property()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var query = WikidataClient.BuildQuery(WikidataClient.ReleaseGroupProperty, ids);

        Assert.Contains("wdt:P436", query);
        Assert.All(ids, id => Assert.Contains($"\"{id}\"", query));
    }
}
