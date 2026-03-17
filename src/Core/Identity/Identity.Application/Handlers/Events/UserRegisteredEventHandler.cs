using MediatR;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Domain.Events;

namespace Product.Template.Core.Identity.Application.Handlers.Events;

/// <summary>
/// Example domain event handler for <see cref="UserRegisteredEvent"/>.
///
/// Domain event handlers are the recommended place to trigger side effects that
/// cross aggregate boundaries — such as sending a welcome email, provisioning
/// default resources, or publishing integration events to a message broker.
///
/// This handler logs the event and illustrates where email dispatch or
/// other notifications would be hooked in production.
/// To send real emails, inject an IEmailSender (or INotificationService) here.
/// </summary>
public sealed class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        // TODO: inject IEmailSender and send a welcome / confirm-email link.
        _logger.LogInformation(
            "User {UserId} registered with e-mail {Email} — welcome email would be sent here",
            notification.UserId,
            notification.Email);

        return Task.CompletedTask;
    }
}
