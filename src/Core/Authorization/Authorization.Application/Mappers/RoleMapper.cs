using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Application.Mappers;

public static class RoleMapper
{
    public static RoleOutput ToOutput(this Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleOutput(role.Id, role.Name, role.Description, role.CreatedAt);
    }

    public static RoleWithPermissionsOutput ToOutputWithPermissions(this Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleWithPermissionsOutput(
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt,
            role.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.ToOutput()));
    }

    public static IEnumerable<RoleOutput> ToOutputList(this IEnumerable<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        return roles.Select(r => r.ToOutput());
    }
}
