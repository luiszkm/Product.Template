using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

/// <summary>
/// Troca um refresh token válido por um novo par (access token + refresh token).
/// O refresh token antigo é automaticamente revogado (token rotation).
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken
) : ICommand<AuthTokenOutput>;

