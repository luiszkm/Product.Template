using Product.Template.Kernel.Domain.Audit;

namespace Product.Template.Kernel.Application.Audit;

/// <summary>
/// Writes audit log entries. Implementations persist to DB, a message bus, or both.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>Writes a single audit entry asynchronously.</summary>
    Task WriteAsync(AuditLog entry, CancellationToken cancellationToken = default);

    /// <summary>Convenience method — builds and writes the entry inline.</summary>
    Task WriteAsync(
        string entityType,
        string? entityId,
        string action,
        string? changes = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}

