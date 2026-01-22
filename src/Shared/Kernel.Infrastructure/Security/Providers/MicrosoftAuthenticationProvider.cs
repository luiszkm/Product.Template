using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kernel.Infrastructure.Security.Providers;

/// <summary>
/// Provedor de autenticação Microsoft (Azure AD / Entra ID)
/// </summary>
public sealed class MicrosoftAuthenticationProvider : IAuthenticationProvider
{
    private readonly MicrosoftAuthSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MicrosoftAuthenticationProvider> _logger;

    public string ProviderName => "microsoft";

    public MicrosoftAuthenticationProvider(
        IOptions<MicrosoftAuthSettings> options,
        HttpClient httpClient,
        ILogger<MicrosoftAuthenticationProvider> logger)
    {
        _settings = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Credentials.TryGetValue("code", out var authCode))
        {
            return new AuthenticationResult(
                false,
                Error: "Authorization code é obrigatório para autenticação Microsoft");
        }

        try
        {
            // 1. Trocar código de autorização por access token
            var tokenResponse = await ExchangeCodeForTokenAsync(authCode, cancellationToken);
            if (tokenResponse == null)
            {
                return new AuthenticationResult(
                    false,
                    Error: "Falha ao obter token da Microsoft");
            }

            // 2. Obter informações do usuário usando o access token
            var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);
            if (userInfo == null)
            {
                return new AuthenticationResult(
                    false,
                    Error: "Falha ao obter informações do usuário da Microsoft");
            }

            _logger.LogInformation(
                "Autenticação Microsoft bem-sucedida para usuário {Email}",
                userInfo.Email);

            return new AuthenticationResult(
                Success: true,
                AccessToken: tokenResponse.AccessToken,
                RefreshToken: tokenResponse.RefreshToken,
                ExpiresIn: tokenResponse.ExpiresIn,
                UserInfo: new Dictionary<string, string>
                {
                    ["email"] = userInfo.Email ?? string.Empty,
                    ["name"] = userInfo.Name ?? string.Empty,
                    ["firstName"] = userInfo.GivenName ?? string.Empty,
                    ["lastName"] = userInfo.Surname ?? string.Empty,
                    ["microsoftId"] = userInfo.Id ?? string.Empty,
                    ["provider"] = ProviderName
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante autenticação Microsoft");
            return new AuthenticationResult(
                false,
                Error: $"Erro na autenticação Microsoft: {ex.Message}");
        }
    }

    public async Task<bool> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            // Validar token fazendo uma requisição ao Microsoft Graph
            var userInfo = await GetUserInfoAsync(token, cancellationToken);
            return userInfo != null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<TokenResponse?> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var tokenEndpoint = $"{_settings.Authority}/oauth2/v2.0/token";

        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _settings.RedirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = _settings.Scopes
        };

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Erro ao trocar código por token. Status: {StatusCode}, Erro: {Error}",
                response.StatusCode,
                errorContent);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
    }

    private async Task<MicrosoftUserInfo?> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        const string graphApiEndpoint = "https://graph.microsoft.com/v1.0/me";

        var request = new HttpRequestMessage(HttpMethod.Get, graphApiEndpoint);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Erro ao obter informações do usuário. Status: {StatusCode}",
                response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>(cancellationToken);
    }

    // DTOs internos
    private record TokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn,
        string? RefreshToken,
        string Scope);

    private record MicrosoftUserInfo(
        string Id,
        string? Email,
        string? Name,
        string? GivenName,
        string? Surname);
}
