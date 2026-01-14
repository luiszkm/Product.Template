using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Core.Identity.Application.Queries.User;

namespace Product.Template.Core.Identity.Application.Handlers.User.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : ICommand<UserOutput>;

