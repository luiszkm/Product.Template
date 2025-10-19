namespace Product.Template.Kernel.Application.Data;

public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken);

    Task Rollback(CancellationToken cancellationToken);
}

