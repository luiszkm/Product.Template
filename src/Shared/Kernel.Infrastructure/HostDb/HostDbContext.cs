using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.HostDb;

public class HostDbContext(DbContextOptions<HostDbContext> options) : DbContext(options)
{
    public DbSet<TenantConfig> Tenants => Set<TenantConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantConfig>(builder =>
        {
            builder.ToTable("Tenants");
            builder.HasKey(x => x.TenantId);
            builder.Property(x => x.TenantId).ValueGeneratedNever();
            builder.Property(x => x.TenantKey).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => x.TenantKey).IsUnique();
            builder.Property(x => x.IsolationMode).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.SchemaName).HasMaxLength(100);
            builder.Property(x => x.ConnectionString).HasMaxLength(1024);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200);
            builder.Property(x => x.ContactEmail).HasMaxLength(256);
            builder.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
