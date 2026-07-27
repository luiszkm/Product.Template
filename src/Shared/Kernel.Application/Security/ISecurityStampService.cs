namespace Product.Template.Kernel.Application.Security;

public interface ISecurityStampService
{
    Task RegenerateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(Guid tenantId, Guid userId, string stamp, CancellationToken cancellationToken = default);
}
