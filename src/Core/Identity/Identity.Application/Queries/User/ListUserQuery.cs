

using Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Application.Queries.User;

public record ListUserQuery() : ListInput,  IQuery<PaginatedListOutput<UserOutput>>;

