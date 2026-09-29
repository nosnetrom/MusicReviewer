using System.Text.Json;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.UnitTests.Catalog;

public class GenreFamiliesTests
{
    private sealed record TaggedArtist(string Name, List<Tag> Genres);

    private sealed record Tag(string Name, int Count);

    /// <summary>Real MusicBrainz genre votes for the 62 featured artists (September 2026).</summary>
    private static readonly Dictionary<string, List<string>> Classified =
        JsonSerializer.Deserialize<List<TaggedArtist>>(Fixture.Read("mb-featured-artist-genres.json"), JsonSerializerOptions.Web)!
            .ToDictionary(
                a => a.Name,
                a => GenreFamilies.Classify(a.Genres.Select(g => (g.Name, g.Count))).Select(f => f.Name).ToList());

    [Theory]
    [InlineData("honky tonk", "Country")]
    [InlineData("bakersfield sound", "Country")]
    [InlineData("western swing", "Country")]
    [InlineData("hard bop", "Jazz")]
    [InlineData("swing", "Jazz")]
    [InlineData("jazz-funk", "Jazz", "Funk & Disco")]
    [InlineData("blues rock", "Rock")]
    [InlineData("pop soul", "Soul & R&B", "Pop")]
    [InlineData("contemporary r&b", "Soul & R&B")]
    [InlineData("jazz rap", "Hip-Hop", "Jazz")]
    [InlineData("rocksteady", "Reggae & Ska")]
    [InlineData("krautrock", "Rock", "Electronic")]
    [InlineData("singer-songwriter")]
    [InlineData("christmas music")]
    public void Maps_specific_tags_to_broad_genres(string tag, params string[] expected) =>
        Assert.Equal(expected, GenreFamilies.Of(tag).Select(f => f.Name));

    [Fact]
    public void Every_featured_artist_gets_at_least_one_and_at_most_three_broad_genres()
    {
        Assert.Equal(62, Classified.Count);
        Assert.All(Classified, a => Assert.InRange(a.Value.Count, 1, GenreFamilies.MaxPerArtist));
    }

    [Theory]
    [InlineData("Miles Davis", "Jazz")]
    [InlineData("Keith Jarrett", "Jazz")]
    [InlineData("Wynton Marsalis", "Jazz")]
    [InlineData("Weather Report", "Jazz")]
    [InlineData("Hank Williams", "Country")]
    [InlineData("Willie Nelson", "Country")]
    [InlineData("Tammy Wynette", "Country")]
    [InlineData("Bob Wills and His Texas Playboys", "Country")]
    [InlineData("Kraftwerk", "Electronic")]
    [InlineData("Bob Marley & The Wailers", "Reggae & Ska")]
    [InlineData("Kendrick Lamar", "Hip-Hop")]
    [InlineData("Aretha Franklin", "Soul & R&B")]
    [InlineData("Mahavishnu Orchestra", "Jazz")]
    // Genuine blends keep their second and third genres.
    [InlineData("Nina Simone", "Jazz", "Soul & R&B")]
    [InlineData("Joni Mitchell", "Folk", "Pop", "Jazz")]
    [InlineData("Emmylou Harris", "Country", "Folk")]
    [InlineData("Allan Holdsworth", "Jazz", "Rock")]
    [InlineData("The Beatles", "Rock", "Pop")]
    public void Classifies_featured_artists_by_weight_of_votes(string artist, params string[] expected) =>
        Assert.Equal(expected, Classified[artist]);

    [Theory]
    // A few "funk" votes beside jazz and jazz fusion (40% of the jazz votes).
    [InlineData("Jaco Pastorius", "Jazz")]
    [InlineData("Snarky Puppy", "Jazz")]
    // "rock", "soft rock" and "country rock" votes (29% of the country votes).
    [InlineData("Shania Twain", "Country", "Pop")]
    // Mostly "blues rock", a style of rock.
    [InlineData("Fleetwood Mac", "Rock")]
    public void Minor_tags_do_not_add_a_second_genre(string artist, params string[] expected) =>
        Assert.Equal(expected, Classified[artist]);

    [Fact]
    public void Umbrella_genres_are_inferred_when_only_subgenres_are_tagged()
    {
        // Return to Forever is tagged "jazz fusion" and "jazz rock" but never plain "jazz".
        Assert.Contains("Jazz", Classified["Return to Forever"]);
        Assert.Contains("Country", Classified["Buck Owens"]);
    }

    [Fact]
    public void A_stray_low_vote_tag_does_not_misfile_an_artist()
    {
        Assert.DoesNotContain("Jazz", Classified["Aretha Franklin"]);
        Assert.DoesNotContain("Blues", Classified["Hank Williams"]);

        var families = GenreFamilies.Classify([("country", 20), ("blues", 2), ("rock", 1)]);
        Assert.Equal([GenreFamilies.Country], families);
    }

    [Fact]
    public void No_votes_means_no_genres() =>
        Assert.Empty(GenreFamilies.Classify([("experimental", 3), ("jazz", 0)]));
}
