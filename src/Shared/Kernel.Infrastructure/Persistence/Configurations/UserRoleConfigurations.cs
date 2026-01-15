using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Kernel.Infrastructure.Persistence.Configurations;

internal class UserRoleConfigurations : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .ValueGeneratedNever();

        builder.Property(ur => ur.UserId)
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .IsRequired();

        // Composite unique index to prevent duplicate user-role assignments
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        // Relationships are defined in User and Role configurations
    }
}
