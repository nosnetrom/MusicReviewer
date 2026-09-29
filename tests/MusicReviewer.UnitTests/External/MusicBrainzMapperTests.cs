using MusicReviewer.Domain.Catalog;
using MusicReviewer.Infrastructure.MusicBrainz;

namespace MusicReviewer.UnitTests.External;

public class MusicBrainzMapperTests
{
    [Fact]
    public void Maps_artist_search_results()
    {
        var response = Fixture.MusicBrainz<MbArtistSearchResponse>("mb-artist-search.json");

        var artists = response.Artists!.Select(MusicBrainzMapper.ToArtistInfo).ToList();

        var miles = artists[0];
        Assert.Equal(Fixture.MilesDavis, miles.MusicBrainzId);
        Assert.Equal("Miles Davis", miles.Name);
        Assert.Equal("Davis, Miles", miles.SortName);
        Assert.Equal(ArtistType.Person, miles.Type);
        Assert.Equal("US", miles.Country);
        Assert.Equal(1926, miles.BeginYear);
        Assert.Equal(1991, miles.EndYear);
        Assert.Equal(100, miles.SearchScore);
        Assert.Contains(artists, a => a is { Name: "Miles Davis Quintet", Type: ArtistType.Group });
    }

    [Fact]
    public void Maps_artist_lookup_with_wikidata_link_and_genres()
    {
        var artist = MusicBrainzMapper.ToArtistInfo(Fixture.MusicBrainz<MbArtist>("mb-artist-lookup.json"));

        Assert.Equal("Q93341", artist.WikidataId);
        Assert.Equal("jazz trumpeter, bandleader, songwriter", artist.Disambiguation);
        Assert.Contains(artist.Genres, g => g is { Name: "jazz", Count: 27 });
        Assert.Contains(artist.Genres, g => g.Name == "modal jazz");
    }

    [Fact]
    public void Maps_a_page_of_studio_release_groups()
    {
        var response = Fixture.MusicBrainz<MbReleaseGroupSearchResponse>("mb-rg-search-studio-p1.json");

        var groups = response.ReleaseGroups!.Select(MusicBrainzMapper.ToReleaseGroupInfo).ToList();

        Assert.Equal(93, response.Count);
        Assert.Equal(25, groups.Count);
        Assert.All(groups, g => Assert.Equal(SecondaryReleaseTypes.None, g.SecondaryTypes));

        var straightNoChaser = Assert.Single(groups, g => g.Title == "Straight-No Chaser!");
        Assert.Equal(ReleaseType.EP, straightNoChaser.PrimaryType);
        Assert.Equal("1958", straightNoChaser.FirstReleaseDate);
        Assert.Equal(Fixture.MilesDavis, straightNoChaser.PrimaryArtistId);

        var siesta = Assert.Single(groups, g => g.Title == "Music From Siesta");
        Assert.Equal("Miles Davis / Marcus Miller", siesta.ArtistCredit);
    }

    [Fact]
    public void Maps_release_tracks_and_personnel()
    {
        var detail = MusicBrainzMapper.ToReleaseDetail(Fixture.MusicBrainz<MbRelease>("mb-release-kind-of-blue.json"));

        Assert.Equal("Columbia", detail.Label);
        Assert.Equal(["So What", "Freddie Freeloader", "Blue in Green", "All Blues", "Flamenco Sketches"], detail.Tracks.Select(t => t.Title));
        Assert.All(detail.Tracks, t => Assert.Equal(1, t.DiscNumber));
        Assert.Equal(545_000, detail.Tracks[0].DurationMs);

        Assert.Contains(detail.Credits, c => c is { PersonName: "John Coltrane", Role: "Performer", Instrument: "tenor saxophone" });
        Assert.Contains(detail.Credits, c => c is { PersonName: "Irving Townsend", Role: "Producer" });
        Assert.Contains(detail.Credits, c => c is { PersonName: "Fred Plaut", Role: "Recording engineer" });

        // Credits repeat on every track in the source; the mapping keeps one of each.
        Assert.Single(detail.Credits, c => c is { PersonName: "Miles Davis", Instrument: "trumpet" });
    }

    [Fact]
    public void Picks_the_original_kind_of_blue_from_a_year_filtered_release_search()
    {
        var response = Fixture.MusicBrainz<MbReleaseSearchResponse>("mb-release-search-kind-of-blue-1959.json");
        var candidates = response.Releases!.Select(MusicBrainzMapper.ToCandidate).ToList();

        var picked = RepresentativeRelease.Pick(candidates, firstReleaseYear: 1959);

        Assert.NotNull(picked);
        Assert.Equal("1959-08-17", picked.Date);
        Assert.Equal("US", picked.Country);
        Assert.Equal(5, picked.TrackCount);
    }

    [Theory]
    [InlineData(new[] { "Live" }, SecondaryReleaseTypes.Live)]
    [InlineData(new[] { "Compilation", "Live" }, SecondaryReleaseTypes.Compilation | SecondaryReleaseTypes.Live)]
    [InlineData(new[] { "DJ-mix", "Mixtape/Street" }, SecondaryReleaseTypes.DjMix | SecondaryReleaseTypes.Mixtape)]
    [InlineData(new string[0], SecondaryReleaseTypes.None)]
    public void Parses_secondary_types(string[] types, SecondaryReleaseTypes expected) =>
        Assert.Equal(expected, MusicBrainzMapper.ParseSecondaryTypes(types));

    [Fact]
    public void Builds_category_queries()
    {
        var studio = MusicBrainzMapper.ReleaseGroupQuery(Fixture.MilesDavis, ReleaseCategories.Studio);
        var live = MusicBrainzMapper.ReleaseGroupQuery(Fixture.MilesDavis, ReleaseCategories.Live);

        Assert.Equal($"arid:{Fixture.MilesDavis} AND (primarytype:album OR primarytype:ep) AND NOT secondarytype:*", studio);
        Assert.EndsWith("AND secondarytype:live", live);
        Assert.Throws<ArgumentOutOfRangeException>(() => MusicBrainzMapper.ReleaseGroupQuery(Fixture.MilesDavis, ReleaseCategories.Studio | ReleaseCategories.Live));
    }

    [Fact]
    public void Escapes_lucene_syntax_in_user_input() =>
        Assert.Equal(@"AC\/DC \(live\) \&\& more\!", MusicBrainzMapper.EscapeLucene("AC/DC (live) && more!"));
}
