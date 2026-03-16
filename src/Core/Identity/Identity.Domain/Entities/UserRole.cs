using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Domain.Entities;

public class UserRole : Entity, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    long IMultiTenantEntity.TenantId
    {
        get => TenantId;
        set => TenantId = value;
    }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole() { }

    private UserRole(Guid id, Guid userId, Guid roleId, long tenantId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        TenantId = tenantId;
        AssignedAt = DateTime.UtcNow;
    }

    public static UserRole Create(Guid userId, Guid roleId, long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        return new UserRole(
            Guid.NewGuid(),
            userId,
            roleId,
            tenantId);
    }
}
