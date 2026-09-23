using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MusicReviewer.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/api/health", new HealthCheckOptions { ResponseWriter = WriteResponse });
        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            [.. report.Entries.Select(e => new HealthCheckResponse(
                e.Key,
                e.Value.Status.ToString(),
                e.Value.Duration.TotalMilliseconds))]));
}

public sealed record HealthResponse(string Status, double TotalDurationMs, IReadOnlyList<HealthCheckResponse> Checks);

public sealed record HealthCheckResponse(string Name, string Status, double DurationMs);
