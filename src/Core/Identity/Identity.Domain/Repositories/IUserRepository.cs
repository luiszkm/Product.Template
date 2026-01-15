using Kernel.Domain.SeedWorks;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Core.Identity.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
