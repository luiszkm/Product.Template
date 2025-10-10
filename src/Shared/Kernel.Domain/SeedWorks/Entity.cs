namespace Product.Template.Kernel.Domain.SeedWorks;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
    protected Entity(TId id)
        => Id = id;
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Id?.Equals(default) == true || other.Id?.Equals(default) == true) return false;
        return Id!.Equals(other.Id);
    }
    public static bool operator ==(Entity<TId> a, Entity<TId> b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }
    public static bool operator !=(Entity<TId> a, Entity<TId> b) => !(a == b);
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
}

