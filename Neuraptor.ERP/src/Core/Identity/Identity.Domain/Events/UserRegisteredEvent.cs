using Neuraptor.ERP.Kernel.Domain.SeedWorks;

namespace Neuraptor.ERP.Core.Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
