using Kernel.Domain.SeedWorks;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Application.Queries.Role;

public record ListRolesQuery() : ListInput, IQuery<PaginatedListOutput<RoleOutput>>;
