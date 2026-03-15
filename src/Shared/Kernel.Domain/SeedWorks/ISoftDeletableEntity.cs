namespace Product.Template.Kernel.Domain.SeedWorks;

/// <summary>
/// Marker interface for entities that support soft deletion.
/// Automatically applied by the global EF query filter — deleted records are invisible to all queries.
/// </summary>
public interface ISoftDeletableEntity
{
    DateTime? DeletedAt { get; }
}

