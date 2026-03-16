using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Domain.Audit;

/// <summary>
/// Immutable record of a state change or business action.
/// Used for compliance, debugging and security analysis.
/// </summary>
public sealed class AuditLog : IMultiTenantEntity
{
    public Guid Id { get; private set; }
    public long TenantId { get; private set; }

    /// <summary>User who triggered the action. "System" for automated processes.</summary>
    public string Actor { get; private set; } = string.Empty;

    /// <summary>Entity type affected (e.g. "User", "Role").</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Primary key of the affected entity.</summary>
    public string? EntityId { get; private set; }

    /// <summary>Action performed (e.g. "Created", "Updated", "Deleted", "Login", "PasswordChanged").</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>JSON snapshot of what changed (old → new values). Null for reads.</summary>
    public string? Changes { get; private set; }

    /// <summary>Extra context (e.g. IP address, correlation ID, reason).</summary>
    public string? Metadata { get; private set; }

    public DateTime OccurredAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Record(
        long tenantId,
        string actor,
        string entityType,
        string? entityId,
        string action,
        string? changes = null,
        string? metadata = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Actor = actor,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Changes = changes,
            Metadata = metadata,
            OccurredAt = DateTime.UtcNow
        };

        log.SetTenant(tenantId);

        return log;
    }

    private void SetTenant(long tenantId)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");
        if (TenantId != 0 && TenantId != tenantId)
            throw new DomainException("TenantId cannot be changed once set.");
        TenantId = tenantId;
    }

    void IMultiTenantEntity.AssignTenant(long tenantId) => SetTenant(tenantId);
}

