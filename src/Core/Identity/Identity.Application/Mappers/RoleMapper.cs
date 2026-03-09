using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Core.Identity.Application.Mappers;

public static class RoleMapper
{
    public static RoleOutput ToOutput(this Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleOutput(role.Id, role.Name, role.Description, role.CreatedAt);
    }

    public static IEnumerable<RoleOutput> ToOutputList(this IEnumerable<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        return roles.Select(r => r.ToOutput());
    }
}

