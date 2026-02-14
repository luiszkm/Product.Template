using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.HostDb;

internal class HostTenantConfiguration : IEntityTypeConfiguration<TenantConfig>
{
    public void Configure(EntityTypeBuilder<TenantConfig> builder)
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
    }
}
