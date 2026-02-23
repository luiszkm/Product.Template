using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyCollection<string>>;
