using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShareShowcase.Api.Data;

/// <summary>
/// Used by <c>dotnet ef</c> at design time so migrations do not require a running app or host configuration.
/// Override with <c>ConnectionStrings__DefaultConnection</c> if your local Postgres differs from Docker Compose defaults.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DefaultLocalConnection =
        "Host=localhost;Port=5432;Database=shareshowcase;Username=shareshowcase;Password=shareshowcase_dev";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultLocalConnection;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
