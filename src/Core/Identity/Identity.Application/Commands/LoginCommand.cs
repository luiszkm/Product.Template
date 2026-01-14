using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Core.Identity.Application.DTOs;

namespace Product.Template.Core.Identity.Application.Commands;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthTokenDto>;

