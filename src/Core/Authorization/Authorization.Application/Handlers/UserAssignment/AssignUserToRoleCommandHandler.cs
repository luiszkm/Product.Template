using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Authorization.Application.Handlers.UserAssignment;

public class AssignUserToRoleCommandHandler : ICommandHandler<AssignUserToRoleCommand>
{
    private readonly IUserAssignmentRepository _assignmentRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ISecurityStampService _securityStampService;
    private readonly ILogger<AssignUserToRoleCommandHandler> _logger;

    public AssignUserToRoleCommandHandler(
        IUserAssignmentRepository assignmentRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ISecurityStampService securityStampService,
        ILogger<AssignUserToRoleCommandHandler> logger)
    {
        _assignmentRepository = assignmentRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _securityStampService = securityStampService;
        _logger = logger;
    }

    public async Task Handle(AssignUserToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role with ID {request.RoleId} not found.");

        var existing = await _assignmentRepository.GetByUserAndRoleAsync(request.UserId, request.RoleId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("User {UserId} already assigned to role {RoleId}", request.UserId, request.RoleId);
            return;
        }

        var tenantId = _tenantContext.TenantId ?? 0;
        if (tenantId <= 0)
            throw new BusinessRuleException("Tenant must be resolved before assigning roles.");

        var assignment = Domain.Entities.UserAssignment.Create(request.UserId, request.RoleId, tenantId, role.Name);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        await _securityStampService.RegenerateAsync(request.UserId, cancellationToken);

        _logger.LogInformation("User {UserId} assigned to role {RoleName}", request.UserId, role.Name);
    }
}
