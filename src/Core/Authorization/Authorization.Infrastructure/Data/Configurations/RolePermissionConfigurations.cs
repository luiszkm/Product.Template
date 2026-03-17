using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Configurations;

internal sealed class RolePermissionConfigurations : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id)
            .ValueGeneratedNever();

        builder.Property(rp => rp.TenantId)
            .IsRequired();

        builder.Property(rp => rp.RoleId)
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .IsRequired();

        builder.Property(rp => rp.AssignedAt)
            .IsRequired();

        builder.HasIndex(rp => new { rp.TenantId, rp.RoleId, rp.PermissionId })
            .IsUnique();
    }
}
