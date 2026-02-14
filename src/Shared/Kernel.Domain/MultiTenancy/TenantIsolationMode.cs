namespace Product.Template.Kernel.Domain.MultiTenancy;

public enum TenantIsolationMode
{
    SharedDb = 0,
    SchemaPerTenant = 1,
    DedicatedDb = 2
}
