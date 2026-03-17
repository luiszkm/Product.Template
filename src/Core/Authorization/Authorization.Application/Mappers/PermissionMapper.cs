using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Entities;

namespace Product.Template.Core.Authorization.Application.Mappers;

public static class PermissionMapper
{
    public static PermissionOutput ToOutput(this Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return new PermissionOutput(permission.Id, permission.Name, permission.Description, permission.CreatedAt);
    }

    public static IEnumerable<PermissionOutput> ToOutputList(this IEnumerable<Permission> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        return permissions.Select(p => p.ToOutput());
    }
}
