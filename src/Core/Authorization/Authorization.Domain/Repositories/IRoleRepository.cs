using Kernel.Domain.SeedWorks;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Domain.Repositories;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
}
