using Microsoft.Extensions.Logging;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role;

public class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RoleOutput> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role with ID {request.RoleId} not found.");

        role.Update(request.Name, request.Description);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("Role {RoleId} updated", role.Id);

        return role.ToOutput();
    }
}
