using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Application.Ingestion;
using MusicReviewer.Infrastructure.CoverArt;
using MusicReviewer.Infrastructure.Ingestion;
using MusicReviewer.Infrastructure.MusicBrainz;
using MusicReviewer.Infrastructure.Persistence;
using MusicReviewer.Infrastructure.Wikidata;
using Polly;

namespace MusicReviewer.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "MusicReviewer";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        AddPersistence(services);
        AddMusicBrainz(services);
        AddWikidataAndCoverArt(services);
        AddIngestion(services);
        return services;
    }

    private static void AddPersistence(IServiceCollection services)
    {
        // Resolve the connection string lazily so test hosts can override configuration.
        services.AddDbContext<MusicReviewerDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            options.UseSqlServer(connectionString, sql => sql
                .EnableRetryOnFailure()
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });
        services.AddScoped<IMusicReviewerDbContext>(sp => sp.GetRequiredService<MusicReviewerDbContext>());

        services.AddHealthChecks().AddDbContextCheck<MusicReviewerDbContext>("database");
    }

    private static void AddMusicBrainz(IServiceCollection services)
    {
        services.AddOptions<MusicBrainzOptions>().BindConfiguration(MusicBrainzOptions.SectionName);

        // The single worker for all MusicBrainz traffic: one instance, also run as a hosted service.
        services.AddSingleton<MusicBrainzGateway>();
        services.AddHostedService(sp => sp.GetRequiredService<MusicBrainzGateway>());
        services.AddTransient<MusicBrainzGatewayHandler>();

        services
            .AddHttpClient<IMusicBrainzClient, MusicBrainzClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
                http.BaseAddress = options.BaseUrl;
                http.DefaultRequestHeaders.UserAgent.Clear();
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // Queue time in the gateway is bounded by callers' tokens; attempts by the pipeline below.
                http.Timeout = Timeout.InfiniteTimeSpan;
            })
            // Outermost: queue in the gateway. Retries then happen inside the caller's slot, with
            // backoff delays (2s+) that keep us under the 1 request/sec limit.
            .AddHttpMessageHandler<MusicBrainzGatewayHandler>()
            .AddResilienceHandler("musicbrainz", (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromSeconds(2),
                });
                pipeline.AddTimeout(options.AttemptTimeout);
            });
    }

    private static void AddWikidataAndCoverArt(IServiceCollection services)
    {
        services.AddOptions<WikidataOptions>().BindConfiguration(WikidataOptions.SectionName);
        services
            .AddHttpClient<IWikidataClient, WikidataClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<WikidataOptions>>().Value;
                http.BaseAddress = options.SparqlEndpoint;
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(40);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            });

        services.AddOptions<CoverArtOptions>().BindConfiguration(CoverArtOptions.SectionName);
        services
            .AddHttpClient<ICoverArtClient, CoverArtClient>(http =>
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", new MusicBrainzOptions().UserAgent))
            // The archive answers with a redirect to the image; we only need to know it exists.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .AddStandardResilienceHandler();
    }

    private static void AddIngestion(IServiceCollection services)
    {
        services.AddOptions<IngestionOptions>().BindConfiguration(IngestionOptions.SectionName);
        services.AddSingleton<IngestionSignal>();
        services.AddScoped<IIngestionScheduler, IngestionScheduler>();
        services.AddHostedService<IngestionWorker>();
        services.AddHostedService<CatalogStartupService>();
    }

    /// <summary>Applies pending EF Core migrations. Intended for local development only.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        await db.Database.MigrateAsync();
    }
}
