using Kernel.Domain.SeedWorks;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Domain.Repositories;

public interface IPermissionRepository : IBaseRepository<Permission>
{
    Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
