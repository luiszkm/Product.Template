using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Queries.UserAssignment;

public record GetUserAssignmentsQuery(Guid UserId) : IQuery<IReadOnlyList<RoleOutput>>;
