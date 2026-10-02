using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicReviewer.Application.Catalog;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Domain.Ingestion;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.IntegrationTests;

public class CatalogEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClient();

    private Task<T?> GetAsync<T>(string url) => _client.GetFromJsonAsync<T>(url, ApiFactory.Json, Ct);

    /// <summary>Polls until the import finishes; imports run on the background worker.</summary>
    private async Task<T> EventuallyAsync<T>(string url, Func<T, bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var value = await GetAsync<T>(url);
            if (value is not null && done(value))
                return value;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"{url} did not finish importing.");
            await Task.Delay(200, Ct);
        }
    }

    [Fact]
    public async Task Imports_an_artist_on_first_view_and_ranks_studio_albums()
    {
        var mbid = FakeMusicBrainz.MilesDavis;

        var artist = await GetAsync<ArtistDetailDto>($"/api/artists/{mbid}");
        Assert.NotNull(artist);
        Assert.Equal("Miles Davis", artist.Name);
        // Broad genres for browsing; the specific MusicBrainz genres are kept as styles.
        Assert.Equal(["jazz"], artist.Genres.Select(g => g.Slug));
        Assert.Equal(["jazz", "modal jazz", "cool jazz"], artist.Styles);

        var studio = await EventuallyAsync<ArtistRecordingsDto>($"/api/artists/{mbid}/recordings", r => r.SyncStatus == SyncStatus.Ready);

        // 27 albums arrived over two 25-item pages; EPs and live albums are filtered out of the default view.
        Assert.Equal(27, studio.Recordings.Count);
        Assert.Equal("Kind of Blue", studio.Recordings[0].Title);
        Assert.True(studio.Recordings[0].HasWikipediaArticle);
        Assert.All(studio.Recordings, r => Assert.StartsWith("https://coverartarchive.org/", r.CoverArtUrl));
        Assert.Contains($"release-groups:{mbid}:Studio:25", factory.MusicBrainz.Calls);

        var eps = await GetAsync<ArtistRecordingsDto>($"/api/artists/{mbid}/recordings?type=ep");
        Assert.Equal(3, eps!.Recordings.Count);

        var byDate = await GetAsync<ArtistRecordingsDto>($"/api/artists/{mbid}/recordings?sort=date");
        Assert.Equal("Studio Album 1", byDate!.Recordings[0].Title);

        var detail = await GetAsync<ArtistDetailDto>($"/api/artists/{mbid}");
        Assert.Equal(SyncStatus.Ready, detail!.SyncStatus);
        Assert.Equal("Miles Davis", detail.WikipediaTitle);

        // The bio arrives with the import, from the article Wikidata links to.
        Assert.NotNull(detail.Summary);
        Assert.Equal("https://en.wikipedia.org/wiki/Miles_Davis", detail.Summary.Url);
        Assert.Equal(1001, detail.Summary.RevisionId);
        Assert.Equal(2, detail.Summary.Paragraphs.Count);
        Assert.StartsWith("Miles Dewey Davis III", detail.Summary.Paragraphs[0]);
    }

    [Fact]
    public async Task Imports_live_albums_only_when_asked_for()
    {
        var mbid = FakeMusicBrainz.MilesDavis;
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{mbid}", a => a.SyncStatus == SyncStatus.Ready);

        var live = await EventuallyAsync<ArtistRecordingsDto>($"/api/artists/{mbid}/recordings?type=live", r => r.SyncStatus == SyncStatus.Ready && r.Recordings.Count > 0);

        var album = Assert.Single(live.Recordings);
        Assert.Equal("Live at the Plugged Nickel", album.Title);
        Assert.Equal(ReleaseCategories.Live, album.Category);
    }

    [Fact]
    public async Task Imports_tracks_and_personnel_on_first_view_of_a_recording()
    {
        var url = $"/api/recordings/{FakeMusicBrainz.KindOfBlue}";

        var recording = await EventuallyAsync<RecordingDetailDto>(url, r => r.DetailsStatus == SyncStatus.Ready);

        Assert.Equal("Kind of Blue", recording.Title);
        Assert.Equal("Miles Davis", recording.ArtistName);
        Assert.Equal("Columbia", recording.Label);
        Assert.Equal(1959, recording.Year);
        Assert.Equal(["So What", "Freddie Freeloader", "Blue in Green", "All Blues", "Flamenco Sketches"], recording.Tracks.Select(t => t.Title));

        Assert.Equal(["Performer", "Producer"], recording.Credits.Select(g => g.Role));
        var performers = recording.Credits[0].People;
        Assert.Contains(performers, p => p is { Name: "John Coltrane", Instruments: "tenor saxophone" });

        Assert.NotNull(recording.Summary);
        Assert.Equal("Kind of Blue", recording.Summary.Title);
        Assert.Equal(["Kind of Blue is a studio album by American jazz musician Miles Davis.", "It is regarded as one of the greatest jazz records."],
            recording.Summary.Paragraphs);
    }

    [Fact]
    public async Task Search_merges_live_results_and_marks_imported_artists()
    {
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{FakeMusicBrainz.MilesDavis}", a => a.SyncStatus == SyncStatus.Ready);

        var result = await GetAsync<SearchResultDto>("/api/search?q=miles");

        Assert.NotNull(result);
        Assert.True(result.RemoteAvailable);
        var miles = Assert.Single(result.Artists);
        Assert.Equal(FakeMusicBrainz.MilesDavis, miles.Mbid);
        Assert.True(miles.IsImported);

        var tooShort = await GetAsync<SearchResultDto>("/api/search?q=m");
        Assert.Empty(tooShort!.Artists);
    }

    [Fact]
    public async Task Search_rejects_queries_over_the_configured_length()
    {
        var query = new string('x', CatalogService.MaxSearchQueryLength + 1);
        var response = await _client.GetAsync($"/api/search?q={Uri.EscapeDataString(query)}", Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("q", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Search_suggestions_list_imported_artists_with_their_genres()
    {
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{FakeMusicBrainz.MilesDavis}", a => a.SyncStatus == SyncStatus.Ready);

        var suggestions = await GetAsync<List<ArtistSuggestionDto>>("/api/search/suggestions");

        var miles = Assert.Single(suggestions!, s => s.Name == "Miles Davis");
        Assert.Equal(["jazz"], miles.Genres);
    }

    [Fact]
    public async Task Repeated_views_queue_a_single_import()
    {
        var mbid = FakeMusicBrainz.JoniMitchell;

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _client.GetAsync($"/api/artists/{mbid}", Ct)));
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{mbid}", a => a.SyncStatus == SyncStatus.Ready);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        var artistId = await db.Artists.Where(a => a.MusicBrainzId == mbid).Select(a => a.Id).SingleAsync(Ct);
        var jobs = await db.IngestionJobs.CountAsync(j => j.TargetId == artistId && j.Type == IngestionJobType.ArtistDiscography, Ct);

        Assert.Equal(1, jobs);
    }

    [Fact]
    public async Task Browse_pages_through_a_genre_with_load_more()
    {
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{FakeMusicBrainz.MilesDavis}", a => a.SyncStatus == SyncStatus.Ready);

        // Miles Davis has 27 studio albums in the fake catalog.
        var first = await GetAsync<BrowseRecordingsDto>("/api/browse/recordings?genre=jazz&limit=10");
        Assert.NotNull(first);
        Assert.Equal(0, first.Offset);
        Assert.Equal(10, first.Recordings.Count);
        Assert.Equal(27, first.Total);
        Assert.True(first.HasMore);
        Assert.Equal("Kind of Blue", first.Recordings[0].Title);

        var seen = first.Recordings.Select(r => r.Mbid).ToList();
        var offset = first.Recordings.Count;
        BrowseRecordingsDto? page;
        do
        {
            page = await GetAsync<BrowseRecordingsDto>($"/api/browse/recordings?genre=jazz&limit=10&offset={offset}");
            seen.AddRange(page!.Recordings.Select(r => r.Mbid));
            offset += page.Recordings.Count;
        }
        while (page.HasMore);

        Assert.Equal(27, seen.Count);
        Assert.Equal(27, seen.Distinct().Count());

        var full = await GetAsync<BrowseRecordingsDto>("/api/browse/recordings?genre=jazz");
        Assert.Equal(27, full!.Recordings.Count); // default page of 60 holds them all
        Assert.False(full.HasMore);
    }

    [Theory]
    [InlineData("?limit=0")]
    [InlineData("?limit=61")]
    [InlineData("?offset=-1")]
    public async Task Invalid_browse_paging_is_400(string query)
    {
        var response = await _client.GetAsync($"/api/browse/recordings{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Outdated_genres_are_reclassified_from_stored_votes_without_calling_MusicBrainz()
    {
        var mbid = FakeMusicBrainz.MilesDavis;
        await EventuallyAsync<ArtistDetailDto>($"/api/artists/{mbid}", a => a.SyncStatus == SyncStatus.Ready);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        var artist = await db.Artists.Include(a => a.Genres).SingleAsync(a => a.MusicBrainzId == mbid, Ct);
        Assert.NotNull(artist.GenreVotes);
        Assert.Equal(GenreFamilies.Version, artist.GenresVersion);

        // Simulate genres classified under an older version of the rules.
        artist.Genres.Clear();
        artist.GenresVersion = 0;
        await db.SaveChangesAsync(Ct);
        var lookupsBefore = factory.MusicBrainz.Calls.Count(c => c == $"artist:{mbid}");

        var result = await scope.ServiceProvider.GetRequiredService<GenreReclassifier>().RunAsync(Ct);

        Assert.True(result.Reclassified >= 1);
        var refreshed = await GetAsync<ArtistDetailDto>($"/api/artists/{mbid}");
        Assert.Equal(["jazz"], refreshed!.Genres.Select(g => g.Slug));
        Assert.Equal(lookupsBefore, factory.MusicBrainz.Calls.Count(c => c == $"artist:{mbid}"));
    }

    [Fact]
    public async Task Unknown_artist_is_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/artists/{Guid.NewGuid()}", Ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem!.Status);
    }

    [Fact]
    public async Task Unavailable_source_is_503_with_retry_after()
    {
        var response = await _client.GetAsync($"/api/artists/{FakeMusicBrainz.Unavailable}", Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Theory]
    [InlineData("?type=singles")]
    [InlineData("?sort=popularity")]
    [InlineData("?type=3")]
    public async Task Invalid_recording_filters_are_400(string query)
    {
        var response = await _client.GetAsync($"/api/artists/{FakeMusicBrainz.MilesDavis}/recordings{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
