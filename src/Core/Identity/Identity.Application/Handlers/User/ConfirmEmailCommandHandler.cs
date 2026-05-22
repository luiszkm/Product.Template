using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.User;

public class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {request.UserId} not found.");

        if (!_emailConfirmationTokenService.ValidateToken(user.Id, user.SecurityStamp, request.Token))
        {
            _logger.LogWarning("Invalid email confirmation token for user {UserId}", request.UserId);
            throw new UnauthorizedAccessException("Invalid email confirmation token.");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("User {UserId} email already confirmed — no-op", request.UserId);
            return;
        }

        user.ConfirmEmail();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("User {UserId} email confirmed", request.UserId);
    }
}
