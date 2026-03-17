using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Product.Template.Kernel.Infrastructure.HostDb;

/// <summary>
/// Enables EF design-time tools (dotnet ef migrations add) to instantiate HostDbContext
/// without the full DI pipeline.
///
/// Connection string priority:
///   1. ConnectionStrings__HostDb environment variable (SQL Server / PostgreSQL for CI/CD)
///   2. Falls back to SQLite (local development, no infrastructure required)
/// </summary>
public class HostDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HostDb");

        var builder = new DbContextOptionsBuilder<HostDbContext>();

        if (!string.IsNullOrWhiteSpace(connectionString)
            && (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)))
        {
            builder.UseSqlServer(connectionString);
        }
        else if (!string.IsNullOrWhiteSpace(connectionString)
            && connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            builder.UseNpgsql(connectionString);
        }
        else
        {
            builder.UseSqlite("Data Source=design-time-host.db");
        }

        return new HostDbContext(builder.Options);
    }
}
