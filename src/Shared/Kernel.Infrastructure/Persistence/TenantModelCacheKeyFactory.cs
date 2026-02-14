using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is AppDbContext appDbContext)
        {
            return (context.GetType(), appDbContext.TenantIdForQueryFilter, designTime);
        }

        return (context.GetType(), designTime);
    }
}
