using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MusicReviewer.Application.Catalog;
using MusicReviewer.Application.Ingestion;

namespace MusicReviewer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<CatalogOptions>().BindConfiguration(CatalogOptions.SectionName);
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<CatalogService>();
        services.AddScoped<FeaturedArtistSeeder>();
        services.AddScoped<GenreReclassifier>();
        services.AddScoped<WikipediaBackfill>();
        services.AddScoped<WikipediaSummaries>();
        services.AddScoped<ArtistImporter>();

        services.AddScoped<IIngestionJobHandler, ArtistDiscographyJobHandler>();
        services.AddScoped<IIngestionJobHandler, ArtistReleaseCategoryJobHandler>();
        services.AddScoped<IIngestionJobHandler, RecordingDetailsJobHandler>();
        services.AddScoped<IIngestionJobHandler, ArtistGenresJobHandler>();
        services.AddScoped<IIngestionJobHandler, WikipediaSummaryJobHandler>();

        return services;
    }
}
