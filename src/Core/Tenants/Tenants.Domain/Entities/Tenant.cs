using Product.Template.Core.Tenants.Domain.Events;
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Domain.Entities;

public class Tenant : AggregateRoot
{
    public string TenantKey { get; private set; }
    public string DisplayName { get; private set; }
    public string? ContactEmail { get; private set; }
    public bool IsActive { get; private set; }
    public TenantIsolationMode IsolationMode { get; private set; }

    private Tenant() { TenantKey = null!; DisplayName = null!; }

    private Tenant(Guid id, string tenantKey, string displayName, string? contactEmail, TenantIsolationMode isolationMode)
    {
        Id = id;
        TenantKey = tenantKey;
        DisplayName = displayName;
        ContactEmail = contactEmail;
        IsActive = true;
        IsolationMode = isolationMode;
    }

    public static Tenant Create(string tenantKey, string displayName, string? contactEmail, TenantIsolationMode isolationMode)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new ArgumentException("TenantKey cannot be empty.", nameof(tenantKey));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName cannot be empty.", nameof(displayName));

        var tenant = new Tenant(
            Guid.NewGuid(),
            tenantKey.Trim().ToLowerInvariant(),
            displayName.Trim(),
            contactEmail?.Trim(),
            isolationMode);

        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.TenantKey));
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
        AddDomainEvent(new TenantDeactivatedEvent(Id, TenantKey));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public static Tenant Reconstitute(
        Guid id,
        string tenantKey,
        string? displayName,
        string? contactEmail,
        bool isActive,
        TenantIsolationMode isolationMode,
        DateTime createdAt)
    {
        return new Tenant
        {
            Id = id,
            TenantKey = tenantKey,
            DisplayName = displayName ?? tenantKey,
            ContactEmail = contactEmail,
            IsActive = isActive,
            IsolationMode = isolationMode,
            CreatedAt = createdAt
        };
    }
}
