using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;

namespace MusicReviewer.Infrastructure.Wikipedia;

public sealed class WikipediaOptions
{
    public const string SectionName = "Wikipedia";

    /// <summary>The MediaWiki Action API, the long-term stable interface (Wikimedia is retiring older REST endpoints).</summary>
    public Uri ApiEndpoint { get; set; } = new("https://en.wikipedia.org/w/api.php");

    /// <summary>Wikimedia requires a descriptive User-Agent with contact details.</summary>
    public string UserAgent { get; set; } = "MusicReviewer/0.1 ( https://github.com/nosnetrom/MusicReviewer )";
}

/// <summary>
/// Fetches plain-text lead sections from English Wikipedia, up to 20 articles per request,
/// following redirects and skipping missing and disambiguation pages.
/// </summary>
public sealed class WikipediaClient(HttpClient http) : IWikipediaClient
{
    public async Task<IReadOnlyDictionary<string, WikipediaLead>> GetLeadsAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken)
    {
        var requested = titles.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (requested.Count == 0)
            return new Dictionary<string, WikipediaLead>();
        if (requested.Count > IWikipediaClient.MaxTitlesPerRequest)
            throw new ArgumentException($"At most {IWikipediaClient.MaxTitlesPerRequest} titles per request.", nameof(titles));

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["action"] = "query",
            ["format"] = "json",
            ["formatversion"] = "2",
            ["prop"] = "extracts|revisions|info|pageprops",
            ["exintro"] = "1",
            ["explaintext"] = "1",
            ["exlimit"] = IWikipediaClient.MaxTitlesPerRequest.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["rvprop"] = "ids",
            ["inprop"] = "url",
            ["ppprop"] = "disambiguation",
            ["redirects"] = "1",
            ["titles"] = string.Join('|', requested),
        });

        using var response = await http.PostAsync((Uri?)null, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceUnavailableException("Wikipedia", new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode));

        var body = await response.Content.ReadFromJsonAsync<WikipediaQueryResponse>(cancellationToken);
        return WikipediaMapper.ToLeads(requested, body);
    }
}

public sealed record WikipediaQueryResponse(WikipediaQuery? Query);

public sealed record WikipediaQuery(List<WikipediaTitleChange>? Normalized, List<WikipediaTitleChange>? Redirects, List<WikipediaPage>? Pages);

public sealed record WikipediaTitleChange(string From, string To);

public sealed record WikipediaPage(
    string Title,
    bool? Missing,
    string? Extract,
    [property: JsonPropertyName("lastrevid")] long? LastRevId,
    [property: JsonPropertyName("fullurl")] string? FullUrl,
    Dictionary<string, string>? PageProps);

public static class WikipediaMapper
{
    /// <summary>Maps each requested title, through normalisation and redirects, to its article's lead.</summary>
    public static IReadOnlyDictionary<string, WikipediaLead> ToLeads(IEnumerable<string> requested, WikipediaQueryResponse? response)
    {
        var query = response?.Query;
        var normalized = (query?.Normalized ?? []).ToDictionary(n => n.From, n => n.To);
        var redirects = (query?.Redirects ?? []).ToDictionary(r => r.From, r => r.To);
        var pages = (query?.Pages ?? [])
            .Where(p => p.Missing != true
                && p.PageProps?.ContainsKey("disambiguation") != true
                && !string.IsNullOrWhiteSpace(p.Extract)
                && p.LastRevId is not null
                && p.FullUrl is not null)
            .ToDictionary(p => p.Title);

        var result = new Dictionary<string, WikipediaLead>();
        foreach (var title in requested)
        {
            var resolved = normalized.GetValueOrDefault(title, title);
            resolved = redirects.GetValueOrDefault(resolved, resolved);
            if (pages.TryGetValue(resolved, out var page))
                result[title] = new WikipediaLead(page.Title, page.FullUrl!, page.LastRevId!.Value, CleanExtract(page.Extract!));
        }

        return result;
    }

    /// <summary>Trims each paragraph and drops blank lines, keeping one newline between paragraphs.</summary>
    public static string CleanExtract(string extract) =>
        string.Join('\n', extract.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
