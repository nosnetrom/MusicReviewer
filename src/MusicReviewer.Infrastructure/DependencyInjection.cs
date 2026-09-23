using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "MusicReviewer";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolve the connection string lazily so test hosts can override configuration.
        services.AddDbContext<MusicReviewerDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });

        services.AddHealthChecks().AddDbContextCheck<MusicReviewerDbContext>("database");

        return services;
    }

    /// <summary>Applies pending EF Core migrations. Intended for local development only.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MusicReviewerDbContext>();
        await db.Database.MigrateAsync();
    }
}
