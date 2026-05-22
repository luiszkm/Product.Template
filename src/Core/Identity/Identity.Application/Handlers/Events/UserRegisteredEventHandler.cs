using MediatR;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Core.Identity.Domain.Events;
using Product.Template.Core.Identity.Domain.Repositories;

namespace Product.Template.Core.Identity.Application.Handlers.Events;

public sealed class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        ILogger<UserRegisteredEventHandler> logger)
    {
        _userRepository = userRepository;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null)
            return;

        var token = _emailConfirmationTokenService.GenerateToken(user.Id, user.SecurityStamp);

        _logger.LogInformation(
            "User {UserId} registered with e-mail {Email} — confirmation e-mail stub dispatched",
            notification.UserId,
            notification.Email);

        _logger.LogDebug(
            "Email confirmation stub for user {UserId}: POST /api/v1/identity/{UserId}/confirm-email body {{ \"token\": \"{Token}\" }}",
            notification.UserId,
            notification.UserId,
            token);
    }
}
