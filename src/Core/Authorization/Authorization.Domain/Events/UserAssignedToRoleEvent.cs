using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Domain.Events;

public record UserAssignedToRoleEvent(Guid UserId, Guid RoleId, string RoleName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
