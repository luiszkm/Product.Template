namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface IMultiTenantEntity
{
    long TenantId { get; set; }
}
