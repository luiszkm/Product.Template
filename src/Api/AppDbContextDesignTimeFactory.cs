using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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
///   1. ConnectionStrings__AppDb environment variable (SQL Server / PostgreSQL for CI/CD)
///   2. Falls back to SQLite (local development, no infrastructure required)
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AppDb");

        var builder = new DbContextOptionsBuilder<AppDbContext>();

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
            builder.UseSqlite("Data Source=design-time.db");
        }

        var registry = new EfModelAssemblyRegistry();
        // Register Kernel.Infrastructure (base configs: User, RefreshToken, AuditLog)
        registry.Register(typeof(AppDbContext).Assembly);
        // Register Authorization.Infrastructure (Role, Permission, RolePermission, UserAssignment)
        registry.Register(typeof(RoleRepository).Assembly);

        return new AppDbContext(builder.Options, new TenantContext(), registry);
    }
}
