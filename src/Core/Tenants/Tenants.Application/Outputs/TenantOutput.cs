using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Tenants.Application.Outputs;

public record TenantOutput(
    Guid TenantId,
    string TenantKey,
    string DisplayName,
    string? ContactEmail,
    bool IsActive,
    TenantIsolationMode IsolationMode,
    DateTime CreatedAt
);
