namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface IMultiTenantEntity
{
    Guid TenantId { get; }
    void AssignTenant(Guid tenantId);
}
