namespace Product.Template.Kernel.Domain.SeedWorks;

/// <summary>
/// Entidade base auditável
/// </summary>
public abstract class AuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Aggregate Root auditável com ID genérico
/// </summary>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    protected AuditableAggregateRoot(TId id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }
}

