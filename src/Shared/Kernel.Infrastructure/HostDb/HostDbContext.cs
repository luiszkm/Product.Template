using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.HostDb;

public class HostDbContext(DbContextOptions<HostDbContext> options) : DbContext(options)
{
    public DbSet<TenantConfig> Tenants => Set<TenantConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HostDbContext).Assembly);
    }
}
