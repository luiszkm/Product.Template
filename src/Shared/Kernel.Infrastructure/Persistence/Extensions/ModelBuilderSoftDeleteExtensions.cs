using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
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

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}

