using Product.Template.Core.Authorization.Domain.Events;
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Domain.Entities;

public class Role : AggregateRoot, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { Name = null!; Description = null!; }

    private Role(Guid id, long tenantId, string name, string description)
    {
        Id = id;
        SetTenant(tenantId);
        Name = name;
        Description = description;
    }

    public static Role Create(long tenantId, string name, string description)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        var role = new Role(Guid.NewGuid(), tenantId, name.Trim(), description ?? string.Empty);
        role.AddDomainEvent(new RoleCreatedEvent(role.Id, role.Name));
        return role;
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description ?? string.Empty;
    }

    public void AssignPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;

        _rolePermissions.Add(RolePermission.Create(Id, permissionId, TenantId));
    }

    public void RevokePermission(Guid permissionId)
    {
        var rp = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rp is not null)
            _rolePermissions.Remove(rp);
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
