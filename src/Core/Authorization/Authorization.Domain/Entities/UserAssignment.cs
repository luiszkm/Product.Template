using Product.Template.Core.Authorization.Domain.Events;
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Domain.Entities;

public class UserAssignment : AggregateRoot, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public Role? Role { get; private set; }

    private UserAssignment() { }

    private UserAssignment(Guid id, Guid userId, Guid roleId, long tenantId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        SetTenant(tenantId);
        AssignedAt = DateTime.UtcNow;
    }

    public static UserAssignment Create(Guid userId, Guid roleId, long tenantId, string roleName)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        if (userId == Guid.Empty)
            throw new DomainException("UserId must be provided.");

        if (roleId == Guid.Empty)
            throw new DomainException("RoleId must be provided.");

        var assignment = new UserAssignment(Guid.NewGuid(), userId, roleId, tenantId);
        assignment.AddDomainEvent(new UserAssignedToRoleEvent(userId, roleId, roleName));
        return assignment;
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
