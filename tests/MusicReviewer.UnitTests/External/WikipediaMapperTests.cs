using MusicReviewer.Infrastructure.Wikipedia;

namespace MusicReviewer.UnitTests.External;

public class WikipediaMapperTests
{
    private static readonly string[] Requested =
        ["Kind of Blue", "Miles Davis", "Kind Of Blue (album)", "No Such Album Xyzzy 1959", "Blue (album)", "Blue Moods"];

    private static IReadOnlyDictionary<string, Application.Abstractions.WikipediaLead> Leads() =>
        WikipediaMapper.ToLeads(Requested, Fixture.Json<WikipediaQueryResponse>("wikipedia-leads.json"));

    [Fact]
    public void Maps_article_leads_with_revision_and_url()
    {
        var kindOfBlue = Leads()["Kind of Blue"];

        Assert.Equal("Kind of Blue", kindOfBlue.Title);
        Assert.Equal("https://en.wikipedia.org/wiki/Kind_of_Blue", kindOfBlue.Url);
        Assert.Equal(1377267828, kindOfBlue.RevisionId);
        Assert.StartsWith("Kind of Blue is a studio album by American jazz musician Miles Davis", kindOfBlue.Extract);

        var paragraphs = kindOfBlue.Extract.Split('\n');
        Assert.Equal(3, paragraphs.Length);
        Assert.All(paragraphs, p => Assert.Equal(p.Trim(), p));
    }

    [Fact]
    public void Leaves_out_missing_and_disambiguation_pages()
    {
        var leads = Leads();

        Assert.Equal(["Blue Moods", "Kind of Blue", "Miles Davis"], leads.Keys.Order());
        // "Blue (album)" redirects to a disambiguation page, which is not a summary.
        Assert.False(leads.ContainsKey("Blue (album)"));
        Assert.False(leads.ContainsKey("No Such Album Xyzzy 1959"));
        Assert.False(leads.ContainsKey("Kind Of Blue (album)"));
    }

    [Fact]
    public void Follows_normalisation_and_redirects_back_to_the_requested_title()
    {
        var response = new WikipediaQueryResponse(new WikipediaQuery(
            Normalized: [new("kind of Blue", "Kind of Blue")],
            Redirects: [new("Kind of Blue", "Kind of Blue (album)")],
            Pages: [new("Kind of Blue (album)", null, "Lead.", 42, "https://en.wikipedia.org/wiki/Kind_of_Blue_(album)", null)]));

        var lead = Assert.Single(WikipediaMapper.ToLeads(["kind of Blue"], response));

        Assert.Equal("kind of Blue", lead.Key);
        Assert.Equal("Kind of Blue (album)", lead.Value.Title);
    }

    [Fact]
    public void Cleans_blank_lines_and_padding_from_extracts() =>
        Assert.Equal("First.\nSecond.", WikipediaMapper.CleanExtract("  First.  \n\n\n Second.\n"));
}
