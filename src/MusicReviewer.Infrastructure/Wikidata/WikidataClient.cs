using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;

namespace MusicReviewer.Infrastructure.Wikidata;

public sealed class WikidataOptions
{
    public const string SectionName = "Wikidata";

    public Uri SparqlEndpoint { get; set; } = new("https://query.wikidata.org/sparql");

    /// <summary>Wikimedia requires a descriptive User-Agent with contact details.</summary>
    public string UserAgent { get; set; } = "MusicReviewer/0.1 ( https://github.com/nosnetrom/MusicReviewer )";

    /// <summary>MusicBrainz IDs per SPARQL query.</summary>
    public int BatchSize { get; set; } = 50;
}

/// <summary>
/// Resolves MusicBrainz IDs to Wikidata items with one SPARQL query per batch, returning each
/// item's English Wikipedia article and sitelink count (how many Wikipedias cover it).
/// </summary>
public sealed class WikidataClient(HttpClient http, IOptions<WikidataOptions> options) : IWikidataClient
{
    /// <summary>Wikidata property "MusicBrainz artist ID".</summary>
    public const string ArtistProperty = "P434";

    /// <summary>Wikidata property "MusicBrainz release group ID".</summary>
    public const string ReleaseGroupProperty = "P436";

    public Task<IReadOnlyDictionary<Guid, WikidataLink>> GetArtistLinksAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken cancellationToken) =>
        GetLinksAsync(ArtistProperty, artistIds, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, WikidataLink>> GetReleaseGroupLinksAsync(IReadOnlyCollection<Guid> releaseGroupIds, CancellationToken cancellationToken) =>
        GetLinksAsync(ReleaseGroupProperty, releaseGroupIds, cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, WikidataLink>> GetLinksAsync(string property, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, WikidataLink>();
        foreach (var batch in ids.Distinct().Chunk(Math.Max(1, options.Value.BatchSize)))
        {
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("query", BuildQuery(property, batch))]);
            using var response = await http.PostAsync((Uri?)null, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceUnavailableException("Wikidata", new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode));

            var body = await response.Content.ReadFromJsonAsync<SparqlResponse>(cancellationToken);
            foreach (var (mbid, link) in WikidataMapper.ToLinks(body))
            {
                // An MBID occasionally appears on more than one item; keep the best-known.
                if (!result.TryGetValue(mbid, out var existing) || link.Sitelinks > existing.Sitelinks)
                    result[mbid] = link;
            }
        }

        return result;
    }

    public static string BuildQuery(string property, IEnumerable<Guid> ids)
    {
        var values = new StringBuilder();
        foreach (var id in ids)
            values.Append(CultureInfo.InvariantCulture, $"\"{id}\" ");

        return $$"""
            SELECT ?mbid ?item ?sitelinks ?article WHERE {
              VALUES ?mbid { {{values}}}
              ?item wdt:{{property}} ?mbid ; wikibase:sitelinks ?sitelinks .
              OPTIONAL { ?article schema:about ?item ; schema:isPartOf <https://en.wikipedia.org/> . }
            }
            """;
    }
}

public sealed record SparqlResponse(SparqlResults? Results);

public sealed record SparqlResults(List<Dictionary<string, SparqlValue>>? Bindings);

public sealed record SparqlValue(string Value);

public static class WikidataMapper
{
    private const string ArticlePrefix = "https://en.wikipedia.org/wiki/";

    public static IEnumerable<(Guid Mbid, WikidataLink Link)> ToLinks(SparqlResponse? response)
    {
        foreach (var binding in response?.Results?.Bindings ?? [])
        {
            if (!binding.TryGetValue("mbid", out var mbid) || !Guid.TryParse(mbid.Value, out var id)
                || !binding.TryGetValue("item", out var item))
                continue;

            var qid = item.Value[(item.Value.LastIndexOf('/') + 1)..];
            var sitelinks = binding.TryGetValue("sitelinks", out var s) && int.TryParse(s.Value, CultureInfo.InvariantCulture, out var n) ? n : 0;
            var title = binding.TryGetValue("article", out var article) ? TitleFromArticleUrl(article.Value) : null;

            yield return (id, new WikidataLink(qid, title, sitelinks));
        }
    }

    /// <summary>"https://en.wikipedia.org/wiki/Kind_of_Blue" → "Kind of Blue".</summary>
    public static string? TitleFromArticleUrl(string url) =>
        url.StartsWith(ArticlePrefix, StringComparison.Ordinal)
            ? Uri.UnescapeDataString(url[ArticlePrefix.Length..]).Replace('_', ' ')
            : null;
}
