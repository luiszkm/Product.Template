using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Role.Commands;

public record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleOutput>;
