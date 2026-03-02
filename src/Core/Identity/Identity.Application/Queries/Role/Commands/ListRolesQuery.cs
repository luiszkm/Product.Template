using Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Identity.Application.Queries.Role.Commands;

public record ListRolesQuery() : ListInput, IQuery<PaginatedListOutput<RoleOutput>>;
