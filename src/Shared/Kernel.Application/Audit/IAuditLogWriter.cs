using Product.Template.Kernel.Domain.Audit;

namespace Product.Template.Kernel.Application.Audit;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLog entry, CancellationToken cancellationToken = default);

    Task WriteAsync(
        string entityType,
        string? entityId,
        string action,
        string? changes = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}

