using Product.Template.Core.Identity.Application.Handlers.Role.Commands;
using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Role;

public class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleOutput> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role not found");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Role name is required.");

        role.Update(request.Name, request.Description);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        return new RoleOutput(role.Id, role.Name, role.Description, role.CreatedAt);
    }
}
