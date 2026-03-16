using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Kernel.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfigurations : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id).ValueGeneratedNever();

        builder.Property(rt => rt.TenantId).IsRequired();

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(rt => new { rt.TenantId, rt.Token }).IsUnique();
        builder.HasIndex(rt => new { rt.TenantId, rt.UserId, rt.IsRevoked });

        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(rt => rt.ReplacedByToken).HasMaxLength(512);
        builder.Property(rt => rt.RevokedByIp).HasMaxLength(64);
        builder.Property(rt => rt.RevokedAt);
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(64).IsRequired();
        builder.Property(rt => rt.CreatedAt).IsRequired();
    }
}

