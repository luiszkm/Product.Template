using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Kernel.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor para preencher automaticamente campos de auditoria
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var currentUser = _currentUserService.UserName ?? "System";
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUser;
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified || HasChangedOwnedEntities(entry))
            {
                entry.Entity.UpdatedBy = currentUser;
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private static bool HasChangedOwnedEntities(EntityEntry entry)
    {
        return entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
    }
}

