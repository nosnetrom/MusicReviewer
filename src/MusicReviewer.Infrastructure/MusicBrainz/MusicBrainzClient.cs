using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;

namespace MusicReviewer.Infrastructure.MusicBrainz;

/// <summary>
/// Typed client for the MusicBrainz web service. The HTTP pipeline routes every request through
/// <see cref="MusicBrainzGateway"/>, then retries transient failures.
/// </summary>
public sealed class MusicBrainzClient(HttpClient http, IOptions<MusicBrainzOptions> options) : IMusicBrainzClient
{
    private int PageSize => Math.Clamp(options.Value.PageSize, 1, 100);

    public async Task<IReadOnlyList<ArtistInfo>> SearchArtistsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var url = $"artist?query={Uri.EscapeDataString(MusicBrainzMapper.EscapeLucene(query))}&limit={Math.Min(limit, PageSize)}&fmt=json";
        var response = await GetAsync<MbArtistSearchResponse>(url, RequestPriority.Interactive, cancellationToken);
        return [.. (response?.Artists ?? []).Select(MusicBrainzMapper.ToArtistInfo)];
    }

    public async Task<ArtistInfo?> GetArtistAsync(Guid artistId, RequestPriority priority, CancellationToken cancellationToken)
    {
        var artist = await GetAsync<MbArtist>($"artist/{artistId}?inc=url-rels+genres&fmt=json", priority, cancellationToken);
        return artist is null ? null : MusicBrainzMapper.ToArtistInfo(artist);
    }

    public async Task<ReleaseGroupInfo?> GetReleaseGroupAsync(Guid releaseGroupId, RequestPriority priority, CancellationToken cancellationToken)
    {
        var group = await GetAsync<MbReleaseGroup>($"release-group/{releaseGroupId}?inc=artist-credits&fmt=json", priority, cancellationToken);
        return group is null ? null : MusicBrainzMapper.ToReleaseGroupInfo(group);
    }

    public async Task<Page<ReleaseGroupInfo>> SearchReleaseGroupsAsync(Guid artistId, ReleaseCategories category, int offset, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString(MusicBrainzMapper.ReleaseGroupQuery(artistId, category));
        var url = $"release-group?query={query}&limit={PageSize}&offset={offset}&fmt=json";
        var response = await GetAsync<MbReleaseGroupSearchResponse>(url, RequestPriority.Background, cancellationToken);

        var items = (response?.ReleaseGroups ?? [])
            // Search matches any credited artist and is fuzzy on types; keep only true matches.
            .Where(g => Recording.CategoryOf(MusicBrainzMapper.ParseSecondaryTypes(g.SecondaryTypes)) == category)
            .Select(MusicBrainzMapper.ToReleaseGroupInfo)
            .ToList();

        return new Page<ReleaseGroupInfo>(response?.Count ?? 0, offset, items, Fetched: response?.ReleaseGroups?.Count ?? 0);
    }

    public async Task<IReadOnlyList<ReleaseCandidate>> GetReleaseCandidatesAsync(Guid releaseGroupId, int? firstReleaseYear, CancellationToken cancellationToken)
    {
        // Prefer a search narrowed to the original year: big release groups have hundreds of editions,
        // and a 25-item browse page would often miss the original.
        if (firstReleaseYear is { } year)
        {
            var y = year.ToString(CultureInfo.InvariantCulture);
            var query = Uri.EscapeDataString($"rgid:{releaseGroupId} AND status:official AND date:[{y} TO {y}-12-31]");
            var search = await GetAsync<MbReleaseSearchResponse>($"release?query={query}&limit={PageSize}&fmt=json", RequestPriority.Background, cancellationToken);
            if (search?.Releases is { Count: > 0 } found)
                return [.. found.Select(MusicBrainzMapper.ToCandidate)];
        }

        var browse = await GetAsync<MbReleaseBrowseResponse>(
            $"release?release-group={releaseGroupId}&status=official&inc=media&limit={PageSize}&fmt=json",
            RequestPriority.Background,
            cancellationToken);
        return [.. (browse?.Releases ?? []).Select(MusicBrainzMapper.ToCandidate)];
    }

    public async Task<ReleaseDetail?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        var release = await GetAsync<MbRelease>(
            $"release/{releaseId}?inc=recordings+labels+artist-rels+recording-level-rels&fmt=json",
            RequestPriority.Background,
            cancellationToken);
        return release is null ? null : MusicBrainzMapper.ToReleaseDetail(release);
    }

    /// <returns>The deserialized body, or null for 404.</returns>
    private async Task<T?> GetAsync<T>(string relativeUrl, RequestPriority priority, CancellationToken cancellationToken)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Options.Set(MusicBrainzGatewayHandler.PriorityKey, priority);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceUnavailableException("MusicBrainz", ex);
        }
        catch (TimeoutException ex)
        {
            throw new ExternalServiceUnavailableException("MusicBrainz", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceUnavailableException("MusicBrainz", new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode));

            return await response.Content.ReadFromJsonAsync<T>(MusicBrainzMapper.JsonOptions, cancellationToken);
        }
    }
}
