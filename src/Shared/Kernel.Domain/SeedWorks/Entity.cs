namespace Product.Template.Kernel.Domain.SeedWorks;

public abstract class Entity<TId> : IAuditableEntity
{
    private bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoredBy { get; set; }
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        CreatedAt = DateTime.UtcNow;
        Id = id;
    }
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

    public void SoftDelete(string deletedBy )
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
    public void Restore(string restoredBy)
    {
        IsDeleted = false;
        DeletedAt = null;
        RestoredAt = DateTime.UtcNow;
        RestoredBy = restoredBy;
    }
}

