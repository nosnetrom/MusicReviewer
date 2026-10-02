using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using MusicReviewer.Api;
using MusicReviewer.Api.Endpoints;
using MusicReviewer.Application;
using MusicReviewer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();
builder.Services.AddOpenApi();
var rateLimit = builder.Configuration.GetSection("RateLimiting");
var tokenLimit = rateLimit.GetValue("TokenLimit", 10);
var tokensPerPeriod = rateLimit.GetValue("TokensPerPeriod", 1);
var replenishmentPeriod = rateLimit.GetValue("ReplenishmentPeriod", TimeSpan.FromSeconds(1));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("catalog", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = tokenLimit,
                TokensPerPeriod = tokensPerPeriod,
                ReplenishmentPeriod = replenishmentPeriod,
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(replenishmentPeriod.TotalSeconds))
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        response.ContentType = "application/problem+json";
        await response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Please wait a moment before trying again.",
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// The SPA is served from a different origin (Static Web Apps) in deployed environments.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.MigrateDatabaseAsync();
}

app.UseCors();
app.UseRateLimiter();

app.MapHealthEndpoints();
app.MapCatalogEndpoints();

app.Run();
