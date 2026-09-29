using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Catalog;

namespace MusicReviewer.Api;

/// <summary>Maps catalog exceptions to RFC 7807 responses: 404 for unknown items, 503 when a source is down.</summary>
public sealed class CatalogExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private const int RetryAfterSeconds = 30;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ExternalServiceUnavailableException unavailable => (StatusCodes.Status503ServiceUnavailable, $"{unavailable.Service} is unavailable"),
            _ => (0, ""),
        };
        if (status == 0)
            return false;

        httpContext.Response.StatusCode = status;
        if (status == StatusCodes.Status503ServiceUnavailable)
            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
