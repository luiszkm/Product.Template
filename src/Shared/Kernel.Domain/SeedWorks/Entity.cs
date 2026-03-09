namespace Product.Template.Kernel.Domain.SeedWorks;

public abstract class Entity : IAuditableEntity
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
    public Guid Id { get; protected set; }
    public long LogicalId { get; protected set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
        Id = Guid.NewGuid();
    }
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id!.Equals(other.Id);
    }
    public static bool operator ==(Entity a, Entity b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }
    public static bool operator !=(Entity a, Entity b) => !(a == b);
    public override int GetHashCode() => Id.GetHashCode();

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

    /// <summary>
    /// For use by seeders and test fixtures only.
    /// Sets a deterministic Id without reflection.
    /// </summary>
    public void SetId(Guid id) => Id = id;
}

