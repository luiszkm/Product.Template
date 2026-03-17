using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.Audit;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Kernel.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically writes an <see cref="AuditLog"/> entry
/// for every Added / Modified / Deleted entity that implements <see cref="IAuditableEntity"/>.
/// </summary>
public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;

    public AuditLogInterceptor(ICurrentUserService currentUserService, ITenantContext tenantContext)
    {
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await WriteAuditEntriesAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            WriteAuditEntriesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();

        return base.SavingChanges(eventData, result);
    }

    private async Task WriteAuditEntriesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var actor = _currentUserService.UserName ?? "System";
        var tenantId = _tenantContext.TenantId ?? 0;
        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Unknown"
            };

            var entityId = GetPrimaryKeyValue(entry);
            var changes = BuildChanges(entry);

            auditEntries.Add(AuditLog.Record(
                tenantId: tenantId,
                actor: actor,
                entityType: entry.Entity.GetType().Name,
                entityId: entityId,
                action: action,
                changes: changes));
        }

        if (auditEntries.Count == 0) return;

        // Add directly to the context set — they'll be saved in the same transaction
        await context.Set<AuditLog>().AddRangeAsync(auditEntries, cancellationToken);
    }

    private static string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;

        var values = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => v is not null);

        return string.Join(",", values);
    }

    private static string? BuildChanges(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return null; // full snapshot is expensive — skip for inserts

        var changes = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            changes[prop.Metadata.Name] = new
            {
                Old = prop.OriginalValue,
                New = prop.CurrentValue
            };
        }

        return changes.Count > 0
            ? JsonSerializer.Serialize(changes)
            : null;
    }
}

