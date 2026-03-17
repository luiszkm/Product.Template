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
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        var registry = new EfModelAssemblyRegistry();
        // Register Kernel.Infrastructure (base configs: User, RefreshToken, AuditLog)
        registry.Register(typeof(AppDbContext).Assembly);
        // Register Authorization.Infrastructure (Role, Permission, RolePermission, UserAssignment)
        registry.Register(typeof(RoleRepository).Assembly);

        return new AppDbContext(options, new TenantContext(), registry);
    }
}
