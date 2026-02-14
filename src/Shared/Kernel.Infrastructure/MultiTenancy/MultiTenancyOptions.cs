namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";
    public string Provider { get; set; } = "InMemory";
    public string? HostDbConnection { get; set; }
    public string? AppDbConnection { get; set; }
    public bool EnableTenantMiddleware { get; set; } = true;
}
