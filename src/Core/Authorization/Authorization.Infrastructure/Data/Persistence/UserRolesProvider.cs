using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Persistence;

public class UserRolesProvider : IUserRolesProvider
{
    private readonly AppDbContext _context;

    public UserRolesProvider(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserRolesData> GetUserRolesAndPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assignments = await _context.Set<UserAssignment>()
            .Where(ua => ua.UserId == userId)
            .ToListAsync(cancellationToken);

        var roleIds = assignments.Select(a => a.RoleId).Distinct().ToList();

        var roles = await _context.Set<Role>()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var roleNames = roles
            .Select(r => r.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissionNames = roles
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission is not null)
            .Select(rp => rp.Permission!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserRolesData(roleNames, permissionNames);
    }
}
