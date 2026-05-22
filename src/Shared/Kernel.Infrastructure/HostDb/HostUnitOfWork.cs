using MediatR;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Kernel.Infrastructure.HostDb;

public sealed class HostUnitOfWork : IHostUnitOfWork
{
    private readonly HostDbContext _context;
    private readonly IPublisher _publisher;

    public HostUnitOfWork(HostDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task Commit(CancellationToken cancellationToken, params AggregateRoot[] aggregates)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(aggregates, cancellationToken);
    }

    public Task Rollback(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private async Task DispatchDomainEventsAsync(
        IReadOnlyCollection<AggregateRoot> aggregates,
        CancellationToken cancellationToken)
    {
        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in events)
            await _publisher.Publish(domainEvent, cancellationToken);
    }
}
