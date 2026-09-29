namespace MusicReviewer.Infrastructure.MusicBrainz;

// Wire shapes of the MusicBrainz JSON web service (https://musicbrainz.org/doc/MusicBrainz_API).
// Property names map via the kebab-case naming policy, e.g. SortName ⇄ "sort-name".

public sealed record MbArtistSearchResponse(int Count, int Offset, List<MbArtist>? Artists);

public sealed record MbArtist(
    Guid Id,
    string Name,
    string? SortName,
    string? Type,
    string? Country,
    string? Disambiguation,
    MbLifeSpan? LifeSpan,
    int? Score,
    List<MbGenre>? Genres,
    List<MbRelation>? Relations);

public sealed record MbLifeSpan(string? Begin, string? End, bool? Ended);

public sealed record MbGenre(string Name, int Count);

public sealed record MbRelation(
    string Type,
    string? TargetType,
    MbUrl? Url,
    MbRelationArtist? Artist,
    List<string>? Attributes);

public sealed record MbUrl(string Resource);

public sealed record MbRelationArtist(Guid Id, string Name);

public sealed record MbReleaseGroupSearchResponse(int Count, int Offset, List<MbReleaseGroup>? ReleaseGroups);

public sealed record MbReleaseGroup(
    Guid Id,
    string Title,
    string? PrimaryType,
    List<string>? SecondaryTypes,
    string? FirstReleaseDate,
    List<MbArtistCredit>? ArtistCredit);

public sealed record MbArtistCredit(string Name, string? Joinphrase, MbRelationArtist? Artist);

public sealed record MbReleaseBrowseResponse(int ReleaseCount, int ReleaseOffset, List<MbRelease>? Releases);

public sealed record MbReleaseSearchResponse(int Count, int Offset, List<MbRelease>? Releases);

public sealed record MbRelease(
    Guid Id,
    string? Status,
    string? Date,
    string? Country,
    List<MbMedium>? Media,
    List<MbLabelInfo>? LabelInfo,
    List<MbRelation>? Relations);

public sealed record MbMedium(int? Position, string? Format, int? TrackCount, List<MbTrack>? Tracks);

public sealed record MbTrack(int Position, string Title, int? Length, MbRecording? Recording);

public sealed record MbRecording(Guid Id, string Title, int? Length, List<MbRelation>? Relations);

public sealed record MbLabelInfo(MbLabel? Label);

public sealed record MbLabel(string Name);
