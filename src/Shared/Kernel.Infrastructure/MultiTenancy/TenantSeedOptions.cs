using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantSeedOptions : System.Collections.Generic.List<TenantSeedDefinition>
{
    public const string SectionName = "Tenants";
}

public class TenantSeedDefinition
{
    public long? TenantId { get; set; }
    public string TenantKey { get; set; } = string.Empty;
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.SharedDb;
    public bool IsActive { get; set; } = true;
    public string? SchemaName { get; set; }
    public string? ConnectionString { get; set; }
}





