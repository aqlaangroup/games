using Microsoft.EntityFrameworkCore;

namespace Platform.Api.Data;

/// <summary>
/// Applies EF Core migrations. On the servers there are two logins: the app runs as games_app
/// (ConnectionStrings:Platform, rows only) and migrates as games_owner (ConnectionStrings:PlatformMigrations), which
/// owns the tables. The owner's default privileges give games_app read and write on them, so migrations need no GRANT.
/// Without PlatformMigrations (Development, tests) the Platform login migrates.
/// </summary>
public static class Migrator
{
    public const string ConnectionName = "PlatformMigrations";

    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var owner = services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName);
        if (string.IsNullOrWhiteSpace(owner))
        {
            await services.GetRequiredService<PlatformDb>().Database.MigrateAsync(ct);
            return;
        }

        await using var db = new PlatformDb(new DbContextOptionsBuilder<PlatformDb>().UseNpgsql(owner).Options);
        await db.Database.MigrateAsync(ct);
    }
}
