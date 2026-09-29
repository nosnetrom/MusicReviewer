using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;

namespace MusicReviewer.Infrastructure.CoverArt;

public sealed class CoverArtOptions
{
    public const string SectionName = "CoverArt";

    public Uri BaseUrl { get; set; } = new("https://coverartarchive.org/");

    /// <summary>Thumbnail size: 250, 500 or 1200.</summary>
    public int Size { get; set; } = 500;
}

/// <summary>
/// Checks the Cover Art Archive for a release group's front cover with a HEAD request.
/// The archive answers 307 (redirect to the image) when one exists and 404 when not; the stable
/// archive URL is stored rather than the redirect target.
/// </summary>
public sealed class CoverArtClient(HttpClient http, IOptions<CoverArtOptions> options) : ICoverArtClient
{
    public async Task<string?> GetFrontCoverUrlAsync(Guid releaseGroupId, CancellationToken cancellationToken)
    {
        var url = new Uri(options.Value.BaseUrl, string.Create(CultureInfo.InvariantCulture, $"release-group/{releaseGroupId}/front-{options.Value.Size}"));
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await http.SendAsync(request, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK or HttpStatusCode.TemporaryRedirect or HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently => url.ToString(),
            HttpStatusCode.NotFound => null,
            _ => throw new ExternalServiceUnavailableException("Cover Art Archive", new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode)),
        };
    }
}
