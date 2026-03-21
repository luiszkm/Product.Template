namespace Kernel.Application.Security;

public interface IAuthenticationProvider
{
    string ProviderName { get; }
    Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

public record AuthenticationRequest(
    string Provider,
    Dictionary<string, string> Credentials);

public record AuthenticationResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    string? Error = null,
    Dictionary<string, string>? UserInfo = null);
