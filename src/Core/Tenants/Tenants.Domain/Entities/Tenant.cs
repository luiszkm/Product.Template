using Product.Template.Core.Tenants.Domain.Events;
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Domain.Entities;

/// <summary>
/// Domain aggregate root for tenant management.
/// NOTE: Tenant does NOT implement IMultiTenantEntity — tenants themselves are not tenant-scoped.
/// Backed by the same "Tenants" table as TenantConfig via TenantRepository mapping.
/// </summary>
public class Tenant : AggregateRoot
{
    public long TenantId { get; private set; }
    public string TenantKey { get; private set; }
    public string DisplayName { get; private set; }
    public string? ContactEmail { get; private set; }
    public bool IsActive { get; private set; }
    public TenantIsolationMode IsolationMode { get; private set; }

    private Tenant() { TenantKey = null!; DisplayName = null!; }

    private Tenant(Guid id, long tenantId, string tenantKey, string displayName, string? contactEmail, TenantIsolationMode isolationMode)
    {
        Id = id;
        TenantId = tenantId;
        TenantKey = tenantKey;
        DisplayName = displayName;
        ContactEmail = contactEmail;
        IsActive = true;
        IsolationMode = isolationMode;
    }

    public static Tenant Create(long tenantId, string tenantKey, string displayName, string? contactEmail, TenantIsolationMode isolationMode)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be a positive number.");

        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new ArgumentException("TenantKey cannot be empty.", nameof(tenantKey));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName cannot be empty.", nameof(displayName));

        var tenant = new Tenant(Guid.NewGuid(), tenantId, tenantKey.Trim().ToLowerInvariant(), displayName.Trim(), contactEmail?.Trim(), isolationMode);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.TenantId, tenant.TenantKey));
        return tenant;
    }

    public void Update(string displayName, string? contactEmail)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName cannot be empty.", nameof(displayName));

        DisplayName = displayName.Trim();
        ContactEmail = contactEmail?.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new TenantDeactivatedEvent(TenantId, TenantKey));
    }

    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Rehydrates a Tenant from persistence (no domain events raised).
    /// </summary>
    public static Tenant Reconstitute(
        long tenantId, string tenantKey, string? displayName, string? contactEmail,
        bool isActive, TenantIsolationMode isolationMode, DateTime createdAt)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantKey = tenantKey,
            DisplayName = displayName ?? tenantKey,
            ContactEmail = contactEmail,
            IsActive = isActive,
            IsolationMode = isolationMode,
            CreatedAt = createdAt
        };
    }
}
