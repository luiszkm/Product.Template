using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Core.Identity.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string revokedByIp, CancellationToken cancellationToken = default);
}

