using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Configurations;

internal sealed class UserAssignmentConfigurations : IEntityTypeConfiguration<UserAssignment>
{
    public void Configure(EntityTypeBuilder<UserAssignment> builder)
    {
        builder.ToTable("UserAssignments");

        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.Id)
            .ValueGeneratedNever();

        builder.Property(ua => ua.TenantId)
            .IsRequired();

        builder.Property(ua => ua.UserId)
            .IsRequired();

        builder.Property(ua => ua.RoleId)
            .IsRequired();

        builder.Property(ua => ua.AssignedAt)
            .IsRequired();

        builder.HasIndex(ua => new { ua.TenantId, ua.UserId, ua.RoleId })
            .IsUnique();

        builder.HasOne(ua => ua.Role)
            .WithMany()
            .HasForeignKey(ua => ua.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
