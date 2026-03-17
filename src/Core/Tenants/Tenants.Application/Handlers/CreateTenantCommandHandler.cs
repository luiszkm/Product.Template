using Microsoft.Extensions.Logging;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Mappers;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Domain.Entities;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Handlers;

public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantOutput>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<TenantOutput> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var existing = await _tenantRepository.GetByKeyAsync(request.TenantKey, cancellationToken);
        if (existing is not null)
            throw new BusinessRuleException($"Tenant with key '{request.TenantKey}' already exists.");

        var tenant = Tenant.Create(
            request.TenantId,
            request.TenantKey,
            request.DisplayName,
            request.ContactEmail,
            request.IsolationMode);

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant {TenantKey} created with ID {TenantId}", tenant.TenantKey, tenant.TenantId);

        return tenant.ToOutput();
    }
}
