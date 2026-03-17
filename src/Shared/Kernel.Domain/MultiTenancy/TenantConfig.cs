namespace Product.Template.Kernel.Domain.MultiTenancy;

public class TenantConfig
{
    public long TenantId { get; set; }
    public string TenantKey { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ContactEmail { get; set; }
    public TenantIsolationMode IsolationMode { get; set; }
    public string? SchemaName { get; set; }
    public string? ConnectionString { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
