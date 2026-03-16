using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Domain.Entities;

public class UserRole : Entity, IMultiTenantEntity
{
    public long TenantId { get; private set; }
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
        SetTenant(tenantId);
        AssignedAt = DateTime.UtcNow;
    }

    public static UserRole Create(Guid userId, Guid roleId, long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        var userRole = new UserRole(
            Guid.NewGuid(),
            userId,
            roleId,
            tenantId);

        return userRole;
    }

    private void SetTenant(long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");
        if (TenantId != 0 && TenantId != tenantId)
            throw new DomainException("TenantId cannot be changed once set.");
        TenantId = tenantId;
    }

    void IMultiTenantEntity.AssignTenant(long tenantId) => SetTenant(tenantId);
}
