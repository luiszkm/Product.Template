using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Api;

/// <summary>
/// Enables EF design-time tools (dotnet ef migrations add) to instantiate AppDbContext
/// without the full DI pipeline. Registers all module EF configuration assemblies so
/// migrations include Authorization tables.
///
/// Connection string priority:
///   1. ConnectionStrings__AppDb environment variable (CI/CD)
///   2. appsettings.json ConnectionStrings:AppDb
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
            ?? BuildConfiguration().GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured.");

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseSqlServer(connectionString);

        var registry = new EfModelAssemblyRegistry();
        registry.Register(typeof(AppDbContext).Assembly);
        registry.Register(typeof(RoleRepository).Assembly);

        return new AppDbContext(builder.Options, new TenantContext(), registry);
    }

    private static IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
}
