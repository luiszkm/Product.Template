using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Identity.Infrastructure.Security;

internal sealed class SecurityStampService : ISecurityStampService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecurityStampService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public SecurityStampService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<SecurityStampService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task RegenerateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(userId), userId);

        user.RegenerateSecurityStamp();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _cache.Remove(CacheKey(tenantId, userId));

        _logger.LogInformation("Security stamp regenerated for user {UserId}", userId);
    }

    public async Task<bool> ValidateAsync(Guid tenantId, Guid userId, string stamp, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey(tenantId, userId), out string? cachedStamp))
            return cachedStamp == stamp;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.TenantId != tenantId)
            return false;

        _cache.Set(CacheKey(tenantId, userId), user.SecurityStamp, CacheTtl);

        return user.SecurityStamp == stamp;
    }

    private static string CacheKey(Guid tenantId, Guid userId) => $"security_stamp_{tenantId}_{userId}";
}
