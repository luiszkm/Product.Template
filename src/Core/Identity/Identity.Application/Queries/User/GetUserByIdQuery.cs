using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Core.Identity.Application.Queries.User;

namespace Product.Template.Core.Identity.Application.Queries.Users;

public record GetUserByIdQuery(Guid UserId) : IQuery<UserOutput>;
