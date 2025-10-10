namespace Product.Template.Kernel.Domain.SeedWorks;

public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    protected AggregateRoot(TId id) : base(id) { }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}

