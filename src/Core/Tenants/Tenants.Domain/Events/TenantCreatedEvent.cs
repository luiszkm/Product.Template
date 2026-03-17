using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Domain.Events;

public record TenantCreatedEvent(long TenantId, string TenantKey) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
