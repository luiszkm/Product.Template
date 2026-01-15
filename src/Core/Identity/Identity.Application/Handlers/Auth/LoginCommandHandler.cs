using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenOutput>
{
    private readonly IUserRepository _userRepository;
    private readonly IHashServices _hashServices;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IHashServices hashServices,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger
        )
    {
        _userRepository = userRepository;
        _hashServices = hashServices;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthTokenOutput> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login attempt failed for non-existing email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        var isPasswordValid = _hashServices.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login attempt failed for email: {Email} due to invalid password", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }


        var token = _jwtTokenService.CreateAccessToken(
                  userId: user.Id,
                  email: user.Email,
                  roles: user.UserRoles.Select(ur => ur.Role.Name).ToList()
              );

        var userAuthOutput = new UserAuthOutput(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastLoginAt: user.LastLoginAt,
            Roles: user.UserRoles.Select(ur => ur.Role.Name).ToList()
        );

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        return new AuthTokenOutput(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            User: userAuthOutput
        );
    }
}

