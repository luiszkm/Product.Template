using Kernel.Domain.SeedWorks;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Application.Queries;

public record ListTenantsQuery(int PageNumber = 1, int PageSize = 20) : ListInput(PageNumber, PageSize), IQuery<PaginatedListOutput<TenantOutput>>;
