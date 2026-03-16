using System.Security.Claims;
using Kernel.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Application.Handlers.Auth;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenOutput>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IHashServices _hashServices;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IHashServices hashServices,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _hashServices = hashServices;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<AuthTokenOutput> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
        {
            _logger.LogWarning("Tentativa de login sem tenant resolvido");
            throw new BusinessRuleException("Tenant must be resolved before login.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login attempt failed for non-existing email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = _hashServices.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login attempt failed for email: {Email} due to invalid password", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role is not null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission is not null)
            .Select(rp => rp.Permission!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissionClaims = permissions
            .Select(permission => new Claim(AuthorizationClaimTypes.Permission, permission));

        var accessToken = _jwtTokenService.CreateAccessToken(
            userId: user.Id,
            email: user.Email,
            roles: roles,
            extraClaims: permissionClaims);

        // Generate and persist refresh token
        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(
            tenantId,
            user.Id,
            rawRefreshToken,
            _jwtTokenService.GetRefreshTokenExpirationDays(),
            clientIp);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        return new AuthTokenOutput(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            RefreshToken: rawRefreshToken,
            User: new UserAuthOutput(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastLoginAt: user.LastLoginAt,
                Roles: roles));
    }
}
