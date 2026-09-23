using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MusicReviewer.Infrastructure;
using Testcontainers.MsSql;

namespace MusicReviewer.IntegrationTests;

/// <summary>
/// Hosts the API in-process against a throwaway SQL Server container (requires Docker).
/// The host runs as Development, so EF Core migrations are applied on startup.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _sql.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _sql.GetConnectionString());

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
