using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Domain.Entities;

public class Permission : Entity, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission() { Name = null!; Description = null!; }

    private Permission(Guid id, long tenantId, string name, string description)
    {
        Id = id;
        SetTenant(tenantId);
        Name = name;
        Description = description;
    }

    public static Permission Create(long tenantId, string name, string description)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        return new Permission(Guid.NewGuid(), tenantId, name.Trim(), description ?? string.Empty);
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description ?? string.Empty;
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
