using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Domain.Entities;

public class UserRole : Entity, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole() { }

    private UserRole(Guid id, Guid userId, Guid roleId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole(
            Guid.NewGuid(),
            userId,
            roleId);
    }
}
