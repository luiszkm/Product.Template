using Kernel.Domain.SeedWorks;

namespace Shared.Kernel.Application.Events;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

}
