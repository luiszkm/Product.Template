using System.Security.Claims;
using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kernel.Infrastructure.Security.Providers;

/// <summary>
/// Provedor de autenticação JWT (Username/Password)
/// </summary>
public sealed class JwtAuthenticationProvider : IAuthenticationProvider
{
    private readonly JwtSettings _settings;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<JwtAuthenticationProvider> _logger;

    public string ProviderName => "jwt";

    public JwtAuthenticationProvider(
        IOptions<JwtSettings> options,
        IJwtTokenService jwtTokenService,
        ILogger<JwtAuthenticationProvider> logger)
    {
        _settings = options.Value;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Este provider não faz autenticação diretamente
        // Ele é usado pelo LoginCommandHandler para gerar tokens
        // A validação de credenciais é feita pelo handler usando IHashServices

        if (!request.Credentials.TryGetValue("userId", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            return Task.FromResult(new AuthenticationResult(
                false,
                Error: "UserId é obrigatório para autenticação JWT"));
        }

        if (!request.Credentials.TryGetValue("email", out var email))
        {
            return Task.FromResult(new AuthenticationResult(
                false,
                Error: "Email é obrigatório para autenticação JWT"));
        }

        // Extrair roles (opcional)
        var roles = request.Credentials.TryGetValue("roles", out var rolesStr)
            ? rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : Array.Empty<string>();

        // Extrair claims extras (opcional)
        List<Claim>? extraClaims = null;
        if (request.Credentials.TryGetValue("extraClaims", out var claimsJson))
        {
            // Aqui você pode deserializar claims extras se necessário
            extraClaims = new List<Claim>();
        }

        try
        {
            var token = _jwtTokenService.CreateAccessToken(
                userId,
                email,
                roles,
                extraClaims);

            var expiresIn = _jwtTokenService.GetExpiresInSeconds();

            _logger.LogInformation("JWT token criado com sucesso para usuário {UserId}", userId);

            return Task.FromResult(new AuthenticationResult(
                Success: true,
                AccessToken: token,
                ExpiresIn: expiresIn,
                UserInfo: new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["email"] = email,
                    ["provider"] = ProviderName
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar token JWT para usuário {UserId}", userId);
            return Task.FromResult(new AuthenticationResult(
                false,
                Error: "Erro ao gerar token de autenticação"));
        }
    }

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // A validação do token JWT é feita pelo middleware de autenticação
        // Este método pode ser usado para validações customizadas adicionais

        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(false);

        // Aqui você pode adicionar validações customizadas
        // Por exemplo: verificar se o token está na blacklist

        return Task.FromResult(true);
    }
}
