using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Product.Template.Kernel.Infrastructure.HostDb;

/// <summary>
/// Enables EF design-time tools (dotnet ef migrations add) to instantiate HostDbContext
/// without the full DI pipeline.
///
/// Connection string priority:
///   1. ConnectionStrings__HostDb environment variable (CI/CD)
///   2. appsettings.json ConnectionStrings:HostDb
/// </summary>
public class HostDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HostDb")
            ?? BuildConfiguration().GetConnectionString("HostDb")
            ?? throw new InvalidOperationException("ConnectionStrings:HostDb is not configured.");

        var builder = new DbContextOptionsBuilder<HostDbContext>();
        builder.UseSqlServer(connectionString);

        return new HostDbContext(builder.Options);
    }

    private static IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
}
