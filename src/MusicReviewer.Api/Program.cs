using System.Text.Json;
using System.Text.Json.Serialization;
using MusicReviewer.Api;
using MusicReviewer.Api.Endpoints;
using MusicReviewer.Application;
using MusicReviewer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();
builder.Services.AddOpenApi();
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

app.MapHealthEndpoints();
app.MapCatalogEndpoints();

app.Run();
