using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

/// <summary>
/// Comando para autenticação via provedor externo (Microsoft, Google, etc.)
/// </summary>
public sealed record ExternalLoginCommand(
    string Provider,
    string Code,
    string? RedirectUri = null) : ICommand<AuthTokenOutput>;
