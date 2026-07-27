using System.Security.Claims;
using Kernel.Application.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;
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
    private readonly IUserRolesProvider _userRolesProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalLoginCommandHandler> _logger;

    public ExternalLoginCommandHandler(
        IAuthenticationProviderFactory providerFactory,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IUserRolesProvider userRolesProvider,
        IConfiguration configuration,
        ILogger<ExternalLoginCommandHandler> logger)
    {
        _providerFactory = providerFactory;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _userRolesProvider = userRolesProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthTokenOutput> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("Tentativa de external login sem tenant resolvido");
            throw new BusinessRuleException("Tenant must be resolved before external login.");
        }

        _logger.LogInformation("Iniciando autenticação externa com provider: {Provider}", request.Provider);

        var provider = _providerFactory.GetProvider(request.Provider);
        if (provider is null)
        {
            _logger.LogWarning("Provider externo desconhecido: {Provider}", request.Provider);
            throw new UnauthorizedAccessException("Authentication provider is not supported.");
        }

        var credentials = new Dictionary<string, string> { ["code"] = request.Code };
        if (!string.IsNullOrEmpty(request.RedirectUri))
            credentials["redirectUri"] = request.RedirectUri;

        var authRequest = new AuthenticationRequest(request.Provider, credentials);
        var authResult = await provider.AuthenticateAsync(authRequest, cancellationToken);

        if (!authResult.Success || authResult.UserInfo == null)
        {
            _logger.LogWarning("Falha na autenticação externa: {Error}", authResult.Error);
            throw new UnauthorizedAccessException(authResult.Error ?? "Autenticação externa falhou");
        }

        if (!authResult.UserInfo.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Provider externo {Provider} não retornou email", request.Provider);
            throw new UnauthorizedAccessException("External provider did not supply a valid email.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            if (!_configuration.GetValue("Identity:AllowExternalLoginAutoProvision", true))
            {
                _logger.LogWarning("External login auto-provision disabled for email: {Email}", email);
                throw new BusinessRuleException("User account does not exist. Contact an administrator.");
            }

            var firstName = authResult.UserInfo.GetValueOrDefault("firstName", "");
            var lastName = authResult.UserInfo.GetValueOrDefault("lastName", "");

            user = Domain.Entities.User.Create(
                tenantId,
                email: email,
                passwordHash: Guid.NewGuid().ToString(),
                firstName: string.IsNullOrEmpty(firstName) ? email.Split('@')[0] : firstName,
                lastName: string.IsNullOrEmpty(lastName) ? "External" : lastName);

            if (_configuration.GetValue("Identity:ConfirmEmailOnExternalProvision", false))
                user.ConfirmEmail();

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Novo usuário criado via autenticação externa: {Email}, Provider: {Provider}",
                email, request.Provider);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("External login failed for inactive user: {Email}", email);
            throw new UnauthorizedAccessException("Autenticação externa falhou");
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("External login failed for unconfirmed email: {Email}", email);
            throw new BusinessRuleException("Email address must be confirmed before login.");
        }

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        var rolesData = await _userRolesProvider.GetUserRolesAndPermissionsAsync(user.Id, cancellationToken);

        var permissionClaims = rolesData.Permissions
            .Select(p => new Claim(AuthorizationClaimTypes.Permission, p));

        var extraClaims = permissionClaims
            .Append(new Claim(AuthorizationClaimTypes.SecurityStamp, user.SecurityStamp))
            .Append(new Claim(AuthorizationClaimTypes.TenantId, tenantId.ToString()));

        var token = _jwtTokenService.CreateAccessToken(
            userId: user.Id,
            email: user.Email,
            roles: rolesData.Roles,
            extraClaims: extraClaims);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = Domain.Entities.RefreshToken.Create(
            tenantId, user.Id, rawRefreshToken,
            _jwtTokenService.GetRefreshTokenExpirationDays(),
            "external-provider");

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation(
            "Autenticação externa bem-sucedida para usuário: {UserId}, Provider: {Provider}",
            user.Id, request.Provider);

        return new AuthTokenOutput(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            RefreshToken: rawRefreshToken,
            User: new UserAuthOutput(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastLoginAt: user.LastLoginAt,
                Roles: rolesData.Roles.ToList()));
    }
}
