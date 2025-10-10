using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Kernel.Application.Events;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

}

