using Neuraptor.ERP.Kernel.Domain.SeedWorks;

namespace Neuraptor.ERP.Core.Identity.Domain.Entities;

public class UserRole : Entity<Guid>
{
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
