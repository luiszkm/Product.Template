using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Domain.Entities;

public class UserRole : Entity<Guid>, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole(Guid id) : base(id) { }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole(Guid.NewGuid())
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        };
    }
}
