using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenOutput>
{
    public async Task<AuthTokenOutput> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implementar lógica de autenticação real
        // Por enquanto, retorna um token mockado
        await Task.Delay(100, cancellationToken); // Simula processamento

        var user = new UserOutput(
            Id: Guid.NewGuid(),
            Email: request.Email,
            FirstName: "Mock",
            LastName: "User",
            EmailConfirmed: true,
            CreatedAt: DateTime.UtcNow,
            LastLoginAt: DateTime.UtcNow,
            Roles: new[] { "User" }
        );

        return new AuthTokenOutput(
            AccessToken: "mock_access_token_" + Guid.NewGuid(),
            TokenType: "Bearer",
            ExpiresIn: 3600,
            User: user
        );
    }
}

