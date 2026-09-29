using System.Collections.Concurrent;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.IntegrationTests;

/// <summary>An in-memory stand-in for MusicBrainz with a small, predictable catalog.</summary>
public sealed class FakeMusicBrainz : IMusicBrainzClient
{
    public const int PageSize = 25;

    public static readonly Guid MilesDavis = Guid.Parse("561d854a-6a28-4aa7-8c99-323e6ce46c2a");
    public static readonly Guid JoniMitchell = Guid.Parse("a6de8ef9-b1a1-4756-97aa-481bbb8a4069");
    public static readonly Guid Unavailable = Guid.Parse("00000000-0000-0000-0000-00000000dead");

    public static readonly Guid KindOfBlue = Guid.Parse("8e8a594f-2175-38c7-a871-abb68ec363e7");
    public static readonly Guid KindOfBlueOriginal = Guid.Parse("79ed3ff2-1b33-3245-8755-947554bc8b3d");
    public static readonly Guid LiveAtPlugged = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly Dictionary<Guid, ArtistInfo> _artists = new()
    {
        [MilesDavis] = new(MilesDavis, "Miles Davis", "Davis, Miles", "jazz trumpeter", ArtistType.Person, "US", 1926, 1991, "Q93341",
            [new("jazz", 27), new("modal jazz", 11), new("cool jazz", 10)], 100),
        [JoniMitchell] = new(JoniMitchell, "Joni Mitchell", "Mitchell, Joni", null, ArtistType.Person, "CA", 1943, null, "Q205721",
            [new("folk", 12)], 100),
    };

    private readonly Dictionary<Guid, List<ReleaseGroupInfo>> _discographies = new()
    {
        [MilesDavis] =
        [
            new(KindOfBlue, "Kind of Blue", "Miles Davis", MilesDavis, ReleaseType.Album, SecondaryReleaseTypes.None, "1959-08-17"),
            .. Enumerable.Range(1, 26).Select(i => new ReleaseGroupInfo(
                Guid.Parse($"22222222-0000-0000-0000-{i:D12}"), $"Studio Album {i}", "Miles Davis", MilesDavis,
                ReleaseType.Album, SecondaryReleaseTypes.None, $"{1950 + i}")),
            .. Enumerable.Range(1, 3).Select(i => new ReleaseGroupInfo(
                Guid.Parse($"33333333-0000-0000-0000-{i:D12}"), $"EP {i}", "Miles Davis", MilesDavis,
                ReleaseType.EP, SecondaryReleaseTypes.None, $"{1960 + i}")),
            new(LiveAtPlugged, "Live at the Plugged Nickel", "Miles Davis", MilesDavis, ReleaseType.Album, SecondaryReleaseTypes.Live, "1965"),
        ],
        [JoniMitchell] =
        [
            new(Guid.Parse("44444444-0000-0000-0000-000000000001"), "Blue", "Joni Mitchell", JoniMitchell, ReleaseType.Album, SecondaryReleaseTypes.None, "1971-06-22"),
        ],
    };

    public ConcurrentBag<string> Calls { get; } = [];

    public Task<IReadOnlyList<ArtistInfo>> SearchArtistsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        Calls.Add($"search:{query}");
        IReadOnlyList<ArtistInfo> results = [.. _artists.Values.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(limit)];
        return Task.FromResult(results);
    }

    public Task<ArtistInfo?> GetArtistAsync(Guid artistId, RequestPriority priority, CancellationToken cancellationToken)
    {
        Calls.Add($"artist:{artistId}");
        if (artistId == Unavailable)
            throw new ExternalServiceUnavailableException("MusicBrainz");
        return Task.FromResult(_artists.GetValueOrDefault(artistId));
    }

    public Task<ReleaseGroupInfo?> GetReleaseGroupAsync(Guid releaseGroupId, RequestPriority priority, CancellationToken cancellationToken) =>
        Task.FromResult(_discographies.Values.SelectMany(d => d).FirstOrDefault(g => g.MusicBrainzId == releaseGroupId));

    public Task<Page<ReleaseGroupInfo>> SearchReleaseGroupsAsync(Guid artistId, ReleaseCategories category, int offset, CancellationToken cancellationToken)
    {
        Calls.Add($"release-groups:{artistId}:{category}:{offset}");
        var all = _discographies.GetValueOrDefault(artistId, []).Where(g => Recording.CategoryOf(g.SecondaryTypes) == category).ToList();
        var items = all.Skip(offset).Take(PageSize).ToList();
        return Task.FromResult(new Page<ReleaseGroupInfo>(all.Count, offset, items, items.Count));
    }

    public Task<IReadOnlyList<ReleaseCandidate>> GetReleaseCandidatesAsync(Guid releaseGroupId, int? firstReleaseYear, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReleaseCandidate> candidates = releaseGroupId == KindOfBlue
            ?
            [
                new(Guid.Parse("ca6b19c7-08b3-4d98-a5db-d73a08562a73"), "1959", "US", ["Reel-to-reel"], 5),
                new(KindOfBlueOriginal, "1959-08-17", "US", ["12\" Vinyl"], 5),
            ]
            : [];
        return Task.FromResult(candidates);
    }

    public Task<ReleaseDetail?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken) =>
        Task.FromResult<ReleaseDetail?>(releaseId == KindOfBlueOriginal
            ? new ReleaseDetail(
                releaseId,
                "Columbia",
                [
                    new(1, 1, "So What", 545_000),
                    new(1, 2, "Freddie Freeloader", 576_000),
                    new(1, 3, "Blue in Green", 329_000),
                    new(1, 4, "All Blues", 694_000),
                    new(1, 5, "Flamenco Sketches", 565_000),
                ],
                [
                    new("Irving Townsend", "Producer", null),
                    new("Miles Davis", "Performer", "trumpet"),
                    new("John Coltrane", "Performer", "tenor saxophone"),
                    new("Bill Evans", "Performer", "piano"),
                    new("Wynton Kelly", "Performer", "piano"),
                ])
            : null);
}

public sealed class FakeWikidata : IWikidataClient
{
    public Task<IReadOnlyDictionary<Guid, WikidataLink>> GetArtistLinksAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, WikidataLink> links = artistIds.Contains(FakeMusicBrainz.MilesDavis)
            ? new Dictionary<Guid, WikidataLink> { [FakeMusicBrainz.MilesDavis] = new("Q93341", "Miles Davis", 99) }
            : new Dictionary<Guid, WikidataLink>();
        return Task.FromResult(links);
    }

    public Task<IReadOnlyDictionary<Guid, WikidataLink>> GetReleaseGroupLinksAsync(IReadOnlyCollection<Guid> releaseGroupIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, WikidataLink> links = releaseGroupIds.Contains(FakeMusicBrainz.KindOfBlue)
            ? new Dictionary<Guid, WikidataLink> { [FakeMusicBrainz.KindOfBlue] = new("Q1340658", "Kind of Blue", 45) }
            : new Dictionary<Guid, WikidataLink>();
        return Task.FromResult(links);
    }
}

public sealed class FakeCoverArt : ICoverArtClient
{
    public Task<string?> GetFrontCoverUrlAsync(Guid releaseGroupId, CancellationToken cancellationToken) =>
        Task.FromResult<string?>($"https://coverartarchive.org/release-group/{releaseGroupId}/front-500");
}

public sealed class FakeWikipedia : IWikipediaClient
{
    private static readonly Dictionary<string, WikipediaLead> Leads = new()
    {
        ["Miles Davis"] = new("Miles Davis", "https://en.wikipedia.org/wiki/Miles_Davis", 1001,
            "Miles Dewey Davis III was an American trumpeter, bandleader, and composer.\nHe is among the most influential figures in jazz."),
        ["Kind of Blue"] = new("Kind of Blue", "https://en.wikipedia.org/wiki/Kind_of_Blue", 2002,
            "Kind of Blue is a studio album by American jazz musician Miles Davis.\nIt is regarded as one of the greatest jazz records."),
    };

    public Task<IReadOnlyDictionary<string, WikipediaLead>> GetLeadsAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, WikipediaLead> leads = titles.Where(Leads.ContainsKey).ToDictionary(t => t, t => Leads[t]);
        return Task.FromResult(leads);
    }
}