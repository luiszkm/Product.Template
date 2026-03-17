using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Kernel.Infrastructure.Persistence.Extensions;

internal static class ModelBuilderSoftDeleteExtensions
{
    /// <summary>
    /// Applies a global query filter to all entities implementing <see cref="ISoftDeletableEntity"/>
    /// so that soft-deleted records are automatically excluded from every query.
    /// </summary>
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(param, nameof(ISoftDeletableEntity.DeletedAt));
            var isNull = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));
            var filter = Expression.Lambda(isNull, param);

            var combinedFilter = CombineWithExistingFilter(entityType, filter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(combinedFilter);
        }
    }

    private static LambdaExpression CombineWithExistingFilter(IMutableEntityType entityType, LambdaExpression newFilter)
    {
#pragma warning disable CS0618
        var existingFilter = entityType.GetQueryFilter();
#pragma warning restore CS0618
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

