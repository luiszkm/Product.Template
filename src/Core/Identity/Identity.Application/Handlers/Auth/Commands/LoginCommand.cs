using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthTokenOutput>;

