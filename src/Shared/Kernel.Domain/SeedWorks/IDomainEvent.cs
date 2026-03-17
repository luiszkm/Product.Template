using MediatR;

namespace Product.Template.Kernel.Domain.SeedWorks;

/// <summary>
/// Marker interface for domain events. Extends <see cref="INotification"/> so that
/// MediatR's <see cref="IPublisher"/> can dispatch events directly without wrapping.
/// Handlers implement <see cref="INotificationHandler{TNotification}"/> where
/// TNotification is the concrete event type (e.g. UserRegisteredEvent).
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
