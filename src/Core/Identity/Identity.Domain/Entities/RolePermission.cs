using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Domain.Entities;

public class RolePermission : Entity, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public Role? Role { get; private set; }
    public Permission? Permission { get; private set; }

    private RolePermission() { }

    private RolePermission(Guid id, Guid roleId, Guid permissionId)
    {
        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
        AssignedAt = DateTime.UtcNow;
    }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission(
            Guid.NewGuid(),
            roleId,
            permissionId);
    }
}
