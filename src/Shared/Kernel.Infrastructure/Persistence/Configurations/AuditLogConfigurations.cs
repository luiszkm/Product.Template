using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Kernel.Domain.Audit;

namespace Product.Template.Kernel.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfigurations : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.Actor).HasMaxLength(256).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(128);
        builder.Property(a => a.Action).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Changes).HasColumnType("text");
        builder.Property(a => a.Metadata).HasMaxLength(1024);
        builder.Property(a => a.OccurredAt).IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
        builder.HasIndex(a => new { a.TenantId, a.Actor });
    }
}

