using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.User;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, UserOutput>
{
    public async Task<UserOutput> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implementar lógica de registro real
        // Por enquanto, retorna um usuário mockado
        await Task.Delay(100, cancellationToken); // Simula processamento

        return new UserOutput(
            Id: Guid.NewGuid(),
            Email: request.Email,
            FirstName: request.FirstName,
            LastName: request.LastName,
            EmailConfirmed: false,
            CreatedAt: DateTime.UtcNow,
            LastLoginAt: null,
            Roles: new[] { "User" }
        );
    }
}

