namespace Product.Template.Kernel.Application.Security;

public interface ISecurityStampService
{
    Task RegenerateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(Guid userId, string stamp, CancellationToken cancellationToken = default);
}
