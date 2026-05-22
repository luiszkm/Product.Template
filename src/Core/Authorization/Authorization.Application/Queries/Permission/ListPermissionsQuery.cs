using Kernel.Domain.SeedWorks;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Application.Queries.Permission;

public record ListPermissionsQuery() : ListInput(PageSize: 50), IQuery<PaginatedListOutput<PermissionOutput>>;
