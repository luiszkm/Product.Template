using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data.Persistence;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetActiveByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<bool> TryRevokeAsync(
        string token,
        string revokedByIp,
        string? replacedByToken,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(
                    rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > now,
                    cancellationToken);

            if (tokenEntity is null)
                return false;

            tokenEntity.Revoke(revokedByIp, replacedByToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var rowsAffected = await _context.RefreshTokens
            .Where(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, now)
                    .SetProperty(rt => rt.RevokedByIp, revokedByIp)
                    .SetProperty(rt => rt.ReplacedByToken, replacedByToken),
                cancellationToken);

        return rowsAffected > 0;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string revokedByIp,
        CancellationToken cancellationToken = default)
    {
        var active = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.Revoke(revokedByIp);
    }
}

