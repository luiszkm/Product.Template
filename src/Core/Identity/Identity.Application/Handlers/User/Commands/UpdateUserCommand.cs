using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.User.Commands;

public record UpdateUserCommand(
    Guid UserId
    ): ICommand<UserOutput>;
