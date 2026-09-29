using System.Text;
using System.Text.Json;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Infrastructure.MusicBrainz;

/// <summary>Translates MusicBrainz wire shapes into application models.</summary>
public static class MusicBrainzMapper
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
    };

    private static readonly Dictionary<string, string> RoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["instrument"] = "Performer",
        ["performer"] = "Performer",
        ["vocal"] = "Vocals",
        ["conductor"] = "Conductor",
        ["arranger"] = "Arranger",
        ["instrument arranger"] = "Arranger",
        ["orchestrator"] = "Arranger",
        ["composer"] = "Composer",
        ["producer"] = "Producer",
        ["engineer"] = "Engineer",
        ["audio"] = "Engineer",
        ["sound"] = "Engineer",
        ["recording"] = "Recording engineer",
        ["mix"] = "Mixing",
        ["mastering"] = "Mastering",
    };

    public static ArtistInfo ToArtistInfo(MbArtist artist) => new(
        artist.Id,
        artist.Name,
        artist.SortName ?? artist.Name,
        string.IsNullOrWhiteSpace(artist.Disambiguation) ? null : artist.Disambiguation,
        ParseArtistType(artist.Type),
        artist.Country,
        PartialDate.Year(artist.LifeSpan?.Begin),
        PartialDate.Year(artist.LifeSpan?.End),
        WikidataIdFrom(artist.Relations),
        [.. (artist.Genres ?? []).Select(g => new GenreInfo(g.Name, g.Count))],
        artist.Score);

    public static ReleaseGroupInfo ToReleaseGroupInfo(MbReleaseGroup group) => new(
        group.Id,
        group.Title,
        FormatCredit(group.ArtistCredit),
        group.ArtistCredit?.FirstOrDefault()?.Artist?.Id,
        ParseReleaseType(group.PrimaryType),
        ParseSecondaryTypes(group.SecondaryTypes),
        string.IsNullOrWhiteSpace(group.FirstReleaseDate) ? null : group.FirstReleaseDate);

    public static ReleaseCandidate ToCandidate(MbRelease release) => new(
        release.Id,
        string.IsNullOrWhiteSpace(release.Date) ? null : release.Date,
        release.Country,
        [.. (release.Media ?? []).Select(m => m.Format).OfType<string>()],
        (release.Media ?? []).Sum(m => m.TrackCount ?? m.Tracks?.Count ?? 0));

    public static ReleaseDetail ToReleaseDetail(MbRelease release)
    {
        var media = release.Media ?? [];
        var tracks = media
            .SelectMany((medium, index) => (medium.Tracks ?? []).Select(t => new TrackInfo(
                medium.Position ?? index + 1,
                t.Position,
                t.Title,
                t.Length ?? t.Recording?.Length)))
            .ToList();

        // Personnel is spread across release-level and per-recording relationships.
        var relations = (release.Relations ?? [])
            .Concat(media.SelectMany(m => m.Tracks ?? []).SelectMany(t => t.Recording?.Relations ?? []));

        var credits = relations
            .Where(r => r.TargetType == "artist" && r.Artist is not null && RoleNames.ContainsKey(r.Type))
            .Select(r => new CreditInfo(
                r.Artist!.Name,
                RoleNames[r.Type],
                r.Attributes is { Count: > 0 } attributes ? string.Join(", ", attributes) : null))
            .Distinct()
            .ToList();

        var label = release.LabelInfo?.Select(l => l.Label?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        return new ReleaseDetail(release.Id, label, tracks, credits);
    }

    public static string? FormatCredit(IReadOnlyList<MbArtistCredit>? credits)
    {
        if (credits is null || credits.Count == 0)
            return null;

        var builder = new StringBuilder();
        foreach (var credit in credits)
            builder.Append(credit.Name).Append(credit.Joinphrase);
        return builder.ToString().Trim();
    }

    public static string? WikidataIdFrom(IEnumerable<MbRelation>? relations)
    {
        var resource = relations?.FirstOrDefault(r => r.Type == "wikidata")?.Url?.Resource;
        if (resource is null)
            return null;

        var id = resource[(resource.LastIndexOf('/') + 1)..];
        return id.StartsWith('Q') ? id : null;
    }

    public static ArtistType ParseArtistType(string? type) => type switch
    {
        "Person" => ArtistType.Person,
        "Group" => ArtistType.Group,
        "Orchestra" => ArtistType.Orchestra,
        "Choir" => ArtistType.Choir,
        "Character" => ArtistType.Character,
        _ => ArtistType.Other,
    };

    public static ReleaseType ParseReleaseType(string? type) => type switch
    {
        "Album" => ReleaseType.Album,
        "Single" => ReleaseType.Single,
        "EP" => ReleaseType.EP,
        "Broadcast" => ReleaseType.Broadcast,
        _ => ReleaseType.Other,
    };

    public static SecondaryReleaseTypes ParseSecondaryTypes(IEnumerable<string>? types)
    {
        var result = SecondaryReleaseTypes.None;
        foreach (var type in types ?? [])
        {
            result |= type switch
            {
                "Compilation" => SecondaryReleaseTypes.Compilation,
                "Soundtrack" => SecondaryReleaseTypes.Soundtrack,
                "Spokenword" => SecondaryReleaseTypes.Spokenword,
                "Interview" => SecondaryReleaseTypes.Interview,
                "Audiobook" => SecondaryReleaseTypes.Audiobook,
                "Audio drama" => SecondaryReleaseTypes.AudioDrama,
                "Live" => SecondaryReleaseTypes.Live,
                "Remix" => SecondaryReleaseTypes.Remix,
                "DJ-mix" => SecondaryReleaseTypes.DjMix,
                "Mixtape/Street" => SecondaryReleaseTypes.Mixtape,
                "Demo" => SecondaryReleaseTypes.Demo,
                "Field recording" => SecondaryReleaseTypes.FieldRecording,
                _ => SecondaryReleaseTypes.None,
            };
        }

        return result;
    }

    /// <summary>Lucene query for one category of an artist's albums and EPs.</summary>
    public static string ReleaseGroupQuery(Guid artistId, ReleaseCategories category)
    {
        var baseQuery = $"arid:{artistId} AND (primarytype:album OR primarytype:ep)";
        return category switch
        {
            ReleaseCategories.Studio => $"{baseQuery} AND NOT secondarytype:*",
            ReleaseCategories.Live => $"{baseQuery} AND secondarytype:live",
            ReleaseCategories.Compilation => $"{baseQuery} AND secondarytype:compilation AND NOT secondarytype:live",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Import one category at a time."),
        };
    }

    /// <summary>Escapes Lucene special characters in user-entered search text.</summary>
    public static string EscapeLucene(string text)
    {
        const string special = "+-&|!(){}[]^\"~*?:\\/";
        var builder = new StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            if (special.Contains(c))
                builder.Append('\\');
            builder.Append(c);
        }

        return builder.ToString();
    }
}
