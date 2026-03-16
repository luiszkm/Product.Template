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

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthTokenOutput>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<AuthTokenOutput> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
        {
            _logger.LogWarning("Tentativa de refresh sem tenant resolvido");
            throw new BusinessRuleException("Tenant must be resolved before refreshing tokens.");
        }

        var existing = await _refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            _logger.LogWarning("Refresh token inválido ou expirado tentou ser usado");
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        if (existing.TenantId != tenantId)
        {
            _logger.LogWarning("Tenant mismatch para refresh token do usuário {UserId}", existing.UserId);
            throw new UnauthorizedAccessException("Refresh token pertence a outro tenant.");
        }

        var user = await _userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Usuário {UserId} do refresh token não encontrado", existing.UserId);
            throw new NotFoundException(nameof(User), existing.UserId);
        }

        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Token rotation: revoke old, issue new
        var newRawToken = _jwtTokenService.GenerateRefreshToken();
        existing.Revoke(clientIp, replacedByToken: newRawToken);

        var newRefreshToken = RefreshToken.Create(
            tenantId,
            user.Id,
            newRawToken,
            _jwtTokenService.GetRefreshTokenExpirationDays(),
            clientIp);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

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
            .Select(p => new Claim(AuthorizationClaimTypes.Permission, p));

        var accessToken = _jwtTokenService.CreateAccessToken(
            userId: user.Id,
            email: user.Email,
            roles: roles,
            extraClaims: permissionClaims);

        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Refresh token rotacionado para usuário {UserId}", user.Id);

        return new AuthTokenOutput(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: _jwtTokenService.GetExpiresInSeconds(),
            RefreshToken: newRawToken,
            User: new UserAuthOutput(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastLoginAt: user.LastLoginAt,
                Roles: roles));
    }
}

