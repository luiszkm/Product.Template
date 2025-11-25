

using Neuraptor.ERP.Kernel.Application.Data;

namespace Neuraptor.ERP.Kernel.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public Task Commit(CancellationToken cancellationToken)
            => _context.SaveChangesAsync(cancellationToken);

        public Task Rollback(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
