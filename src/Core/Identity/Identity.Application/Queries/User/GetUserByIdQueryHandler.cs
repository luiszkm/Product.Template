using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserOutput>
{
    public async Task<UserOutput> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implementar busca real no repositório
        // Por enquanto, retorna um usuário mockado
        await Task.Delay(50, cancellationToken); // Simula consulta no banco

        return new UserOutput(
            Id: request.UserId,
            Email: "user@example.com",
            FirstName: "John",
            LastName: "Doe",
            EmailConfirmed: true,
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            LastLoginAt: DateTime.UtcNow.AddHours(-2),
            Roles: new[] { "User", "Admin" }
        );
    }
}
