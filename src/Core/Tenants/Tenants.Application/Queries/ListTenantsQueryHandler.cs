using Microsoft.Extensions.Logging;
using Product.Template.Core.Tenants.Application.Mappers;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.Tenants.Application.Queries;

public class ListTenantsQueryHandler : IQueryHandler<ListTenantsQuery, PaginatedListOutput<TenantOutput>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<ListTenantsQueryHandler> _logger;

    public ListTenantsQueryHandler(ITenantRepository tenantRepository, ILogger<ListTenantsQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<PaginatedListOutput<TenantOutput>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing tenants — page {PageNumber}, size {PageSize}", request.PageNumber, request.PageSize);

        var tenants = await _tenantRepository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<TenantOutput>(
            PageNumber: tenants.PageNumber,
            PageSize: tenants.PageSize,
            TotalCount: tenants.TotalCount,
            Data: tenants.Data.ToOutputList().ToList());
    }
}
