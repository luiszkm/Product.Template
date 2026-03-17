using Microsoft.Extensions.Logging;
using Product.Template.Core.Tenants.Application.Mappers;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Queries;

public class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantOutput>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<GetTenantByIdQueryHandler> _logger;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository, ILogger<GetTenantByIdQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<TenantOutput> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching tenant by ID: {TenantId}", request.TenantId);

        var tenant = await _tenantRepository.GetByTenantIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant with ID {request.TenantId} not found.");

        return tenant.ToOutput();
    }
}
