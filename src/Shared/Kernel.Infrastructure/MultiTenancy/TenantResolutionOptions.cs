namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantResolutionOptions
{
    public const string SectionName = "MultiTenancy";
    public string HeaderName { get; set; } = "X-Tenant";
    public bool AllowPublicFallback { get; set; }
    public string PublicTenantKey { get; set; } = "public";
}
