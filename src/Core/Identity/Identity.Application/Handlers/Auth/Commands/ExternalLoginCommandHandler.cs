using Kernel.Application.Security;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

/// <summary>
/// Handler para autenticação via provedores externos (Microsoft, Google, etc.)
/// </summary>
public sealed class ExternalLoginCommandHandler : ICommandHandler<ExternalLoginCommand, AuthTokenOutput>
{
    private readonly IAuthenticationProviderFactory _providerFactory;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ExternalLoginCommandHandler> _logger;

    public ExternalLoginCommandHandler(
        IAuthenticationProviderFactory providerFactory,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<ExternalLoginCommandHandler> logger)
    {
        _providerFactory = providerFactory;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<AuthTokenOutput> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
        {
            _logger.LogWarning("Tentativa de external login sem tenant resolvido");
            throw new BusinessRuleException("Tenant must be resolved before external login.");
        }

        _logger.LogInformation(
            "Iniciando autenticação externa com provider: {Provider}",
            request.Provider);

        // 1. Obter o provider específico
        var provider = _providerFactory.GetProvider(request.Provider);
        if (provider is null)
        {
            _logger.LogWarning("Provider externo desconhecido: {Provider}", request.Provider);
            throw new UnauthorizedAccessException("Authentication provider is not supported.");
        }

        // 2. Preparar credenciais para o provider
        var credentials = new Dictionary<string, string>
        {
            ["code"] = request.Code
        };

        if (!string.IsNullOrEmpty(request.RedirectUri))
            credentials["redirectUri"] = request.RedirectUri;

        // 3. Autenticar via provider externo
        var authRequest = new AuthenticationRequest(request.Provider, credentials);
        var authResult = await provider.AuthenticateAsync(authRequest, cancellationToken);

        if (!authResult.Success || authResult.UserInfo == null)
        {
            _logger.LogWarning(
                "Falha na autenticação externa: {Error}",
                authResult.Error);
            throw new UnauthorizedAccessException(authResult.Error ?? "Autenticação externa falhou");
        }

        // 4. Obter ou criar usuário no sistema
        if (!authResult.UserInfo.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Provider externo {Provider} não retornou email", request.Provider);
            throw new UnauthorizedAccessException("External provider did not supply a valid email.");
        }
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            // Criar novo usuário a partir dos dados do provider externo
            var firstName = authResult.UserInfo.GetValueOrDefault("firstName", "");
            var lastName = authResult.UserInfo.GetValueOrDefault("lastName", "");

            user = Domain.Entities.User.Create(
                tenantId,
                email: email,
                passwordHash: Guid.NewGuid().ToString(), // Password aleatório para contas externas
                firstName: string.IsNullOrEmpty(firstName) ? email.Split('@')[0] : firstName,
                lastName: string.IsNullOrEmpty(lastName) ? "External" : lastName
            );

            user.ConfirmEmail(); // Auto-confirmar email de providers confiáveis

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Novo usuário criado via autenticação externa: {Email}, Provider: {Provider}",
                email,
                request.Provider);
        }

        // 5. Atualizar último login
        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        // 6. Gerar token JWT interno
        var roles = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var token = _jwtTokenService.CreateAccessToken(
            userId: user.Id,
            email: user.Email,
            roles: roles
        );

        var userAuthOutput = new UserAuthOutput(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastLoginAt: user.LastLoginAt,
            Roles: roles
        );

        _logger.LogInformation(
            "Autenticação externa bem-sucedida para usuário: {UserId}, Provider: {Provider}",
            user.Id,
            request.Provider);

        // Gerar refresh token para autenticação externa também
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var clientIp = "external-provider"; // Placeholder - idealmente usar IHttpContextAccessor

        var refreshToken = Domain.Entities.RefreshToken.Create(
            tenantId,
            user.Id,
            rawRefreshToken,
            _jwtTokenService.GetRefreshTokenExpirationDays(),
            clientIp);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        return new AuthTokenOutput(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            RefreshToken: rawRefreshToken,
            User: userAuthOutput
        );
    }
}
