using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Application.Abstractions;

/// <summary>
/// MusicBrainz web service. Every call is funnelled through a single rate-limited worker,
/// so callers may wait; interactive calls are served before background ones.
/// </summary>
public interface IMusicBrainzClient
{
    Task<IReadOnlyList<ArtistInfo>> SearchArtistsAsync(string query, int limit, CancellationToken cancellationToken);

    /// <returns>null when MusicBrainz has no such artist.</returns>
    Task<ArtistInfo?> GetArtistAsync(Guid artistId, RequestPriority priority, CancellationToken cancellationToken);

    /// <returns>null when MusicBrainz has no such release group.</returns>
    Task<ReleaseGroupInfo?> GetReleaseGroupAsync(Guid releaseGroupId, RequestPriority priority, CancellationToken cancellationToken);

    /// <summary>One page of an artist's albums and EPs in the given category.</summary>
    Task<Page<ReleaseGroupInfo>> SearchReleaseGroupsAsync(Guid artistId, ReleaseCategories category, int offset, CancellationToken cancellationToken);

    /// <summary>Official releases (editions) of a release group, earliest-first where MusicBrainz can filter.</summary>
    Task<IReadOnlyList<ReleaseCandidate>> GetReleaseCandidatesAsync(Guid releaseGroupId, int? firstReleaseYear, CancellationToken cancellationToken);

    Task<ReleaseDetail?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken);
}

public enum RequestPriority
{
    /// <summary>A user is waiting on the response (search, first page view).</summary>
    Interactive,

    /// <summary>Import jobs.</summary>
    Background,
}

/// <summary>Links MusicBrainz IDs to Wikidata items and their English Wikipedia articles.</summary>
public interface IWikidataClient
{
    Task<IReadOnlyDictionary<Guid, WikidataLink>> GetArtistLinksAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, WikidataLink>> GetReleaseGroupLinksAsync(IReadOnlyCollection<Guid> releaseGroupIds, CancellationToken cancellationToken);
}

public interface ICoverArtClient
{
    /// <returns>The front-cover image URL, or null when the release group has no cover art.</returns>
    Task<string?> GetFrontCoverUrlAsync(Guid releaseGroupId, CancellationToken cancellationToken);
}

/// <summary>An external data source could not be reached or is rate-limiting us.</summary>
public sealed class ExternalServiceUnavailableException(string service, Exception? inner = null)
    : Exception($"{service} is currently unavailable.", inner)
{
    public string Service { get; } = service;
}
