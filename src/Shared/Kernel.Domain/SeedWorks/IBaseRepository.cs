

using Product.Template.Kernel.Domain.SeedWorks;

namespace Kernel.Domain.SeedWorks;

public interface IBaseRepository <T>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);


    Task<PaginatedListOutput<T>> ListAllAsync(
        ListInput listInput,
        CancellationToken cancellationToken = default);
}
