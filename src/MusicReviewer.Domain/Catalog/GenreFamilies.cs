namespace MusicReviewer.Domain.Catalog;

/// <summary>A broad genre used for browsing, e.g. Jazz or Country.</summary>
public sealed record GenreFamily(string Name, string Slug);

/// <summary>
/// Maps MusicBrainz's fine-grained, community-voted genres ("hard bop", "honky tonk",
/// "jazz-funk") onto a small set of broad genres, and picks an artist's broad genres
/// from the weight of votes so that a stray low-vote tag doesn't misfile them.
/// </summary>
public static class GenreFamilies
{
    public static readonly GenreFamily Jazz = new("Jazz", "jazz");
    public static readonly GenreFamily Blues = new("Blues", "blues");
    public static readonly GenreFamily Rock = new("Rock", "rock");
    public static readonly GenreFamily Pop = new("Pop", "pop");
    public static readonly GenreFamily Soul = new("Soul & R&B", "soul-rnb");
    public static readonly GenreFamily Funk = new("Funk & Disco", "funk-disco");
    public static readonly GenreFamily HipHop = new("Hip-Hop", "hip-hop");
    public static readonly GenreFamily Country = new("Country", "country");
    public static readonly GenreFamily Folk = new("Folk", "folk");
    public static readonly GenreFamily Electronic = new("Electronic", "electronic");
    public static readonly GenreFamily Reggae = new("Reggae & Ska", "reggae-ska");
    public static readonly GenreFamily Gospel = new("Gospel", "gospel");

    private static readonly GenreFamily[] Ordered =
        [Jazz, Blues, Rock, Pop, Soul, Funk, HipHop, Country, Folk, Electronic, Reggae, Gospel];

    public static IReadOnlyList<GenreFamily> All => Ordered;

    /// <summary>
    /// Bump whenever the mapping or thresholds change; artists classified under an older
    /// version are re-classified at startup.
    /// </summary>
    public const int Version = 2;

    /// <summary>
    /// A secondary broad genre must carry at least this share of the strongest genre's votes.
    /// High enough that a few stray votes ("funk" on a jazz act, "rock" on a country singer)
    /// don't add a genre; low enough to keep real blends (Joni Mitchell: folk, pop, jazz).
    /// </summary>
    public const double MinimumShareOfTop = 0.45;

    public const int MaxPerArtist = 3;

    /// <summary>Tags whose wording would mislead the keyword rules below, or that need two families.</summary>
    private static readonly Dictionary<string, GenreFamily[]> Explicit = new(StringComparer.OrdinalIgnoreCase)
    {
        ["western swing"] = [Country],
        ["swing"] = [Jazz],
        ["big band"] = [Jazz],
        ["dixieland"] = [Jazz],
        ["third stream"] = [Jazz],
        ["jazz rap"] = [HipHop, Jazz],
        ["rockabilly"] = [Rock, Country],
        // A style of rock: counting it toward Blues filed rock bands (Fleetwood Mac) under Blues.
        ["blues rock"] = [Rock],
        ["southern rock"] = [Rock],
        ["british rhythm & blues"] = [Rock, Blues],
        ["rhythm & blues"] = [Soul, Blues],
        ["rhythm and blues"] = [Soul, Blues],
        ["merseybeat"] = [Rock, Pop],
        ["krautrock"] = [Rock, Electronic],
        ["synth-pop"] = [Electronic, Pop],
        ["trip hop"] = [Electronic, HipHop],
        // Roles and occasions rather than sounds: kept as styles, never used for browsing.
        ["singer-songwriter"] = [],
        ["americana"] = [Country, Folk],
        ["bluegrass"] = [Country],
        ["honky tonk"] = [Country],
        ["nashville sound"] = [Country],
        ["bakersfield sound"] = [Country],
        ["western"] = [Country],
        ["country yodeling"] = [Country],
        ["motown"] = [Soul],
        ["disco"] = [Funk],
        ["electro"] = [Electronic],
        ["ska"] = [Reggae],
        ["rocksteady"] = [Reggae],
        ["dub"] = [Reggae],
        ["dancehall"] = [Reggae],
        ["christmas music"] = [],
        ["experimental"] = [],
        ["classical"] = [],
        ["modern classical"] = [],
    };

    /// <summary>Word fragments and the broad genre each implies; a tag can match several ("folk pop").</summary>
    private static readonly (string Fragment, GenreFamily Family)[] Keywords =
    [
        ("hip hop", HipHop), ("hip-hop", HipHop), ("rap", HipHop),
        ("jazz", Jazz), ("bop", Jazz),
        ("blues", Blues),
        ("soul", Soul), ("r&b", Soul),
        ("funk", Funk),
        ("country", Country),
        ("folk", Folk),
        ("reggae", Reggae),
        ("gospel", Gospel),
        ("electronic", Electronic), ("techno", Electronic), ("house", Electronic), ("trance", Electronic),
        ("ambient", Electronic), ("synth", Electronic),
        ("rock", Rock), ("punk", Rock), ("metal", Rock), ("grunge", Rock),
        ("pop", Pop),
    ];

    /// <summary>The broad genres a single MusicBrainz tag belongs to (possibly none).</summary>
    public static IReadOnlyList<GenreFamily> Of(string tag)
    {
        var key = tag.Trim().ToLowerInvariant();
        if (Explicit.TryGetValue(key, out var explicitFamilies))
            return explicitFamilies;

        return [.. Keywords.Where(k => key.Contains(k.Fragment, StringComparison.Ordinal)).Select(k => k.Family).Distinct()];
    }

    /// <summary>
    /// An artist's broad genres, strongest first: each family scores the votes of every tag
    /// mapped to it, and families below <see cref="MinimumShareOfTop"/> of the top score are dropped.
    /// </summary>
    public static IReadOnlyList<GenreFamily> Classify(IEnumerable<(string Tag, int Votes)> tags)
    {
        var scores = new Dictionary<GenreFamily, int>();
        foreach (var (tag, votes) in tags)
        {
            if (votes <= 0)
                continue;
            foreach (var family in Of(tag))
                scores[family] = scores.GetValueOrDefault(family) + votes;
        }

        if (scores.Count == 0)
            return [];

        var top = scores.Values.Max();
        return
        [
            .. scores
                .Where(s => s.Value >= top * MinimumShareOfTop)
                .OrderByDescending(s => s.Value)
                .ThenBy(s => Array.IndexOf(Ordered, s.Key))
                .Take(MaxPerArtist)
                .Select(s => s.Key),
        ];
    }
}
