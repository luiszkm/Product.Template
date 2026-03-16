using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
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

            var combinedFilter = CombineWithExistingFilter(entityType, filter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(combinedFilter);
        }
    }

    private static LambdaExpression CombineWithExistingFilter(IMutableEntityType entityType, LambdaExpression newFilter)
    {
        var existingFilter = entityType.GetQueryFilter();
        if (existingFilter is null)
        {
            return newFilter;
        }

        var parameter = Expression.Parameter(entityType.ClrType!, "e");

        var left = ReplacingExpressionVisitor.Replace(existingFilter.Parameters[0], parameter, existingFilter.Body);
        var right = ReplacingExpressionVisitor.Replace(newFilter.Parameters[0], parameter, newFilter.Body);

        return Expression.Lambda(Expression.AndAlso(left, right), parameter);
    }
}
