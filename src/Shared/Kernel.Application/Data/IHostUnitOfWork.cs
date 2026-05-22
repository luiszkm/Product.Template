using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Kernel.Application.Data;

public interface IHostUnitOfWork
{
    Task Commit(CancellationToken cancellationToken, params AggregateRoot[] aggregates);

    Task Rollback(CancellationToken cancellationToken);
}
