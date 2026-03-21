using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Domain.Audit;

public sealed class AuditLog : IMultiTenantEntity
{
    public Guid Id { get; private set; }
    public long TenantId { get; private set; }
    public string Actor { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Changes { get; private set; }
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

