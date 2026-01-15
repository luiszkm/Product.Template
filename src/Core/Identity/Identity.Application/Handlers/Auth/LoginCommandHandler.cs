using Kernel.Application.Security;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenOutput>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenOutput> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implementar lógica de autenticação real
        // Por enquanto, retorna um token mockado
        await Task.Delay(100, cancellationToken); // Simula processamento

        var user = new UserAuthOutput(
            Id: Guid.NewGuid(),
            FirstName: "Mock",
            Email: "teste@email",
            LastLoginAt: DateTime.UtcNow,
            Roles: new[] { "User" }
        );

        // Gera o token JWT real
        var token = _jwtTokenService.CreateAccessToken(
            userId: user.Id,
            email: user.Email,
            roles: user.Roles
        );

        return new AuthTokenOutput(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            User: user
        );
    }
}

