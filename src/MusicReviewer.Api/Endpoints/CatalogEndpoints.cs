using MusicReviewer.Application.Catalog;

namespace MusicReviewer.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Catalog");

        api.MapGet("/search", (string? q, CatalogService catalog, CancellationToken ct) => catalog.SearchAsync(q, ct))
            .WithSummary("Search artists: local matches plus a live MusicBrainz search.");

        api.MapGet("/artists/{mbid:guid}", (Guid mbid, CatalogService catalog, CancellationToken ct) => catalog.GetArtistAsync(mbid, ct))
            .WithSummary("Artist details. The first request for an artist queues its discography import.");

        api.MapGet("/artists/{mbid:guid}/recordings", GetArtistRecordingsAsync)
            .WithSummary("An artist's recordings. type: studio | ep | live | compilation | all; sort: notability | date.");

        api.MapGet("/recordings/{mbid:guid}", (Guid mbid, CatalogService catalog, CancellationToken ct) => catalog.GetRecordingAsync(mbid, ct))
            .WithSummary("Recording details, track list and personnel.");

        api.MapGet("/browse/featured", (CatalogService catalog, CancellationToken ct) => catalog.GetFeaturedAsync(ct));

        api.MapGet("/genres", (CatalogService catalog, CancellationToken ct) => catalog.GetGenresAsync(ct));

        api.MapGet("/browse/recordings", (string? genre, string? decade, CatalogService catalog, CancellationToken ct) =>
                catalog.BrowseRecordingsAsync(genre, decade, ct))
            .WithSummary("Studio albums across imported artists, filtered by genre slug and/or decade (e.g. 1970s).");

        return app;
    }

    private static async Task<IResult> GetArtistRecordingsAsync(Guid mbid, string? type, string? sort, CatalogService catalog, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        var filter = RecordingFilter.Studio;
        if (type is not null && !TryParseName(type, out filter))
            errors["type"] = ["Use studio, ep, live, compilation or all."];

        var order = RecordingSort.Notability;
        if (sort is not null && !TryParseName(sort, out order))
            errors["sort"] = ["Use notability or date."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        return TypedResults.Ok(await catalog.GetArtistRecordingsAsync(mbid, filter, order, ct));
    }

    /// <summary>Parses an enum by name only, so numeric strings like "3" are rejected.</summary>
    private static bool TryParseName<T>(string value, out T result)
        where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result) && !char.IsDigit(value.TrimStart()[0]);
}
