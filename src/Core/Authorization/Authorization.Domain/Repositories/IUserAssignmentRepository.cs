using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Domain.Repositories;

public interface IUserAssignmentRepository
{
    Task<UserAssignment?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAssignment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserAssignment assignment, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserAssignment assignment, CancellationToken cancellationToken = default);
}
