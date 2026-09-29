using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Infrastructure;
using Testcontainers.MsSql;

namespace MusicReviewer.IntegrationTests;

/// <summary>
/// Hosts the API in-process against a throwaway SQL Server container (requires Docker), with
/// external music sources replaced by fakes. The host runs as Development, so EF Core
/// migrations are applied on startup; featured-artist seeding is switched off.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public FakeMusicBrainz MusicBrainz { get; } = new();

    public async ValueTask InitializeAsync() => await _sql.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _sql.GetConnectionString());
        builder.UseSetting("Catalog:SeedFeaturedArtistsOnStartup", "false");
        builder.UseSetting("Ingestion:PollInterval", "00:00:00.200");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMusicBrainzClient>();
            services.RemoveAll<IWikidataClient>();
            services.RemoveAll<ICoverArtClient>();
            services.RemoveAll<IWikipediaClient>();
            services.AddSingleton<IMusicBrainzClient>(MusicBrainz);
            services.AddSingleton<IWikidataClient, FakeWikidata>();
            services.AddSingleton<ICoverArtClient, FakeCoverArt>();
            services.AddSingleton<IWikipediaClient, FakeWikipedia>();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
