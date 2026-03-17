using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.Audit;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence.Extensions;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly EfModelAssemblyRegistry? _registry;

    public long TenantIdForQueryFilter => _tenantContext.Tenant?.IsolationMode == TenantIsolationMode.SharedDb
        ? _tenantContext.TenantId ?? 0
        : 0;

    // Identity Tables
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Cross-cutting Tables
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext, EfModelAssemblyRegistry registry) : base(options)
    {
        _tenantContext = tenantContext;
        _registry = registry;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (_registry is not null)
        {
            foreach (var assembly in _registry.Assemblies)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
        }

        modelBuilder.ApplyTenantQueryFilters(this);
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
