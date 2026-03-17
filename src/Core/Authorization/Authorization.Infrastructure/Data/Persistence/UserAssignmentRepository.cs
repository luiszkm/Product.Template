using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Persistence;

public class UserAssignmentRepository : IUserAssignmentRepository
{
    private readonly AppDbContext _context;

    public UserAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserAssignment?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        => await _context.Set<UserAssignment>()
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<UserAssignment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Set<UserAssignment>()
            .Where(ua => ua.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserAssignment assignment, CancellationToken cancellationToken = default)
        => await _context.Set<UserAssignment>().AddAsync(assignment, cancellationToken);

    public Task DeleteAsync(UserAssignment assignment, CancellationToken cancellationToken = default)
    {
        _context.Set<UserAssignment>().Remove(assignment);
        return Task.CompletedTask;
    }
}
