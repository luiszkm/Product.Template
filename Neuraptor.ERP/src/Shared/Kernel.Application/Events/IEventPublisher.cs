using Neuraptor.ERP.Kernel.Domain.SeedWorks;

namespace Neuraptor.ERP.Kernel.Application.Events;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

}

