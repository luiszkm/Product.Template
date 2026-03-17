using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Queries.Role;

public record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleWithPermissionsOutput>;
