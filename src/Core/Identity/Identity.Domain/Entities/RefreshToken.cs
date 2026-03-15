using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Domain.Entities;

public class RefreshToken : Entity, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? RevokedByIp { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string token,
        int expirationDays,
        string createdByIp)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            IsRevoked = false,
            CreatedByIp = createdByIp,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(string revokedByIp, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}

