using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Domain.Entities;

public class RolePermission : Entity, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public Role? Role { get; private set; }
    public Permission? Permission { get; private set; }

    private RolePermission() { }

    private RolePermission(Guid id, Guid roleId, Guid permissionId, long tenantId)
    {
        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
        SetTenant(tenantId);
        AssignedAt = DateTime.UtcNow;
    }

    public static RolePermission Create(Guid roleId, Guid permissionId, long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        return new RolePermission(Guid.NewGuid(), roleId, permissionId, tenantId);
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
