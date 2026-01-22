namespace Kernel.Application.Security;

/// <summary>
/// Interface base para provedores de autenticação
/// Permite implementar diferentes estratégias de autenticação (JWT, OAuth, SAML, etc.)
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Nome único do provedor (ex: "jwt", "microsoft", "google")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Autentica um usuário usando as credenciais fornecidas
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se um token ainda é válido
    /// </summary>
    Task<bool> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request genérico para autenticação
/// </summary>
public record AuthenticationRequest(
    string Provider,
    Dictionary<string, string> Credentials);

/// <summary>
/// Resultado da autenticação
/// </summary>
public record AuthenticationResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    string? Error = null,
    Dictionary<string, string>? UserInfo = null);
