using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Domain.Entities;

public class Permission : Entity, IMultiTenantEntity
{
    public long TenantId { get; private set; }
    long IMultiTenantEntity.TenantId
    {
        get => TenantId;
        set => TenantId = value;
    }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission() { }

    private Permission(Guid id, long tenantId, string name, string description)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public static Permission Create(long tenantId, string name, string description)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));

        return new Permission(
            Guid.NewGuid(),
            tenantId,
            name.Trim(),
            description ?? string.Empty);
    }
}
