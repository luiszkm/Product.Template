using Kernel.Domain.SeedWorks;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Authorization.Application.Queries.Permission;

public record ListPermissionsQuery(int PageNumber = 1, int PageSize = 50) : ListInput(PageNumber, PageSize), IQuery<PaginatedListOutput<PermissionOutput>>;
