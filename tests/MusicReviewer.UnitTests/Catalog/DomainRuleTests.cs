using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.UnitTests.Catalog;

public class NameNormalizerTests
{
    [Theory]
    [InlineData("Björk", "bjork")]
    [InlineData("BJORK", "bjork")]
    [InlineData("  The   Beatles ", "the beatles")]
    [InlineData("AC/DC", "ac dc")]
    [InlineData("Guns N' Roses", "guns n roses")]
    [InlineData("Sigur Rós", "sigur ros")]
    [InlineData("Bob Marley & The Wailers", "bob marley the wailers")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalizes_for_search(string? input, string expected) =>
        Assert.Equal(expected, NameNormalizer.Normalize(input));

    [Fact]
    public void Setting_an_artist_name_updates_its_search_key()
    {
        var artist = new Artist { SortName = "Simone, Nina", Name = "Nina Simone" };

        Assert.Equal("nina simone", artist.NormalizedName);
    }
}

public class PartialDateTests
{
    [Theory]
    [InlineData("1959", 1959, 1)]
    [InlineData("1959-08", 1959, 2)]
    [InlineData("1959-08-17", 1959, 3)]
    [InlineData("", null, 0)]
    [InlineData(null, null, 0)]
    [InlineData("n/a", null, 0)]
    public void Parses_year_and_precision(string? value, int? year, int precision)
    {
        Assert.Equal(year, PartialDate.Year(value));
        Assert.Equal(precision, PartialDate.Precision(value));
    }
}

public class NotabilityTests
{
    private static Recording Album(string title, SecondaryReleaseTypes secondary = SecondaryReleaseTypes.None, ReleaseType type = ReleaseType.Album) =>
        new() { Title = title, PrimaryType = type, SecondaryTypes = secondary };

    [Fact]
    public void A_widely_covered_album_outranks_an_obscure_one()
    {
        var landmark = Album("Kind of Blue");
        landmark.WikipediaTitle = "Kind of Blue";
        landmark.WikidataSitelinks = 45;
        var obscure = Album("Modern Jazz Trumpets");

        Assert.True(Notability.Score(landmark) > Notability.Score(obscure) + 30);
    }

    [Fact]
    public void Studio_albums_outrank_eps_live_albums_and_compilations_all_else_equal()
    {
        var studio = Notability.Score(Album("A"));

        Assert.True(studio > Notability.Score(Album("B", type: ReleaseType.EP)));
        Assert.True(studio > Notability.Score(Album("C", SecondaryReleaseTypes.Live)));
        Assert.True(studio > Notability.Score(Album("D", SecondaryReleaseTypes.Compilation)));
    }

    [Fact]
    public void An_english_article_matters_more_than_a_few_extra_sitelinks()
    {
        var withArticle = Album("A");
        withArticle.WikipediaTitle = "A";
        withArticle.WikidataSitelinks = 2;
        var withoutArticle = Album("B");
        withoutArticle.WikidataSitelinks = 5;

        Assert.True(Notability.Score(withArticle) > Notability.Score(withoutArticle));
    }

    [Theory]
    [InlineData(SecondaryReleaseTypes.None, ReleaseCategories.Studio)]
    [InlineData(SecondaryReleaseTypes.Live, ReleaseCategories.Live)]
    [InlineData(SecondaryReleaseTypes.Compilation, ReleaseCategories.Compilation)]
    [InlineData(SecondaryReleaseTypes.Live | SecondaryReleaseTypes.Compilation, ReleaseCategories.Live)]
    [InlineData(SecondaryReleaseTypes.Soundtrack, ReleaseCategories.None)]
    public void Categorizes_by_secondary_type(SecondaryReleaseTypes secondary, ReleaseCategories expected) =>
        Assert.Equal(expected, Recording.CategoryOf(secondary));
}

public class RepresentativeReleaseTests
{
    private static ReleaseCandidate Candidate(string id, string? date, string? country, string format, int tracks = 5) =>
        new(Guid.Parse(id), date, country, [format], tracks);

    [Fact]
    public void Prefers_a_fully_dated_release_from_the_original_year()
    {
        var picked = RepresentativeRelease.Pick(
            [
                Candidate("00000000-0000-0000-0000-000000000001", "1997-03-25", "US", "CD", 6),
                Candidate("00000000-0000-0000-0000-000000000002", "1959", "US", "Reel-to-reel"),
                Candidate("00000000-0000-0000-0000-000000000003", "1959-08-17", "US", "12\" Vinyl"),
            ],
            firstReleaseYear: 1959);

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000003"), picked?.Id);
    }

    [Fact]
    public void Breaks_ties_by_country_then_format()
    {
        var picked = RepresentativeRelease.Pick(
            [
                Candidate("00000000-0000-0000-0000-000000000001", "1971-06-22", "JP", "12\" Vinyl"),
                Candidate("00000000-0000-0000-0000-000000000002", "1971-06-22", "US", "Cassette"),
                Candidate("00000000-0000-0000-0000-000000000003", "1971-06-22", "US", "12\" Vinyl"),
            ],
            firstReleaseYear: 1971);

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000003"), picked?.Id);
    }

    [Fact]
    public void Ignores_releases_without_tracks_and_handles_no_candidates()
    {
        Assert.Null(RepresentativeRelease.Pick([Candidate("00000000-0000-0000-0000-000000000001", "1959", "US", "CD", tracks: 0)], 1959));
        Assert.Null(RepresentativeRelease.Pick([], 1959));
    }
}
