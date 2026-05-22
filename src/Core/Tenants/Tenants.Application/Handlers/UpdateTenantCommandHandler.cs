using Microsoft.Extensions.Logging;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Mappers;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Handlers;

public class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand, TenantOutput>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IHostUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IHostUnitOfWork unitOfWork,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TenantOutput> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByTenantIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant with ID {request.TenantId} not found.");

        tenant.Update(request.DisplayName, request.ContactEmail);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.Commit(cancellationToken, tenant);

        _logger.LogInformation("Tenant {TenantId} updated", tenant.Id);

        return tenant.ToOutput();
    }
}
