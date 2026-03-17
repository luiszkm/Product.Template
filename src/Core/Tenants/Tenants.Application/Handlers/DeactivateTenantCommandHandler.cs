using Microsoft.Extensions.Logging;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Handlers;

public class DeactivateTenantCommandHandler : ICommandHandler<DeactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<DeactivateTenantCommandHandler> _logger;

    public DeactivateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ILogger<DeactivateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByTenantIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant with ID {request.TenantId} not found.");

        tenant.Deactivate();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} deactivated", tenant.TenantId);
    }
}
