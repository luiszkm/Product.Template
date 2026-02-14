using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.Persistence.Extensions;

internal static class ModelBuilderTenantExtensions
{
    public static void ApplyTenantQueryFilters(this ModelBuilder modelBuilder, AppDbContext dbContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null || !typeof(IMultiTenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entityParameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantProperty = Expression.Property(entityParameter, nameof(IMultiTenantEntity.TenantId));
            var contextConstant = Expression.Constant(dbContext);
            var tenantIdFromContext = Expression.Property(contextConstant, nameof(AppDbContext.TenantIdForQueryFilter));
            var equalExpression = Expression.Equal(tenantProperty, tenantIdFromContext);
            var filter = Expression.Lambda(equalExpression, entityParameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
