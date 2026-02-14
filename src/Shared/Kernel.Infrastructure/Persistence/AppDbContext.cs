using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence.Extensions;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public long TenantIdForQueryFilter => _tenantContext.Tenant?.IsolationMode == TenantIsolationMode.SharedDb
        ? _tenantContext.TenantId ?? 0
        : 0;

    // Identity Tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyTenantQueryFilters(this);
    }
}
