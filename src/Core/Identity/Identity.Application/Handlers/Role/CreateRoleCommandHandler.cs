using Product.Template.Core.Identity.Application.Handlers.Role.Commands;
using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Role;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleOutput>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleOutput> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Role name is required.");

        var existing = await _roleRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            throw new BusinessRuleException($"Role '{name}' already exists.");

        var role = Domain.Entities.Role.Create(name, request.Description);

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        return new RoleOutput(role.Id, role.Name, role.Description, role.CreatedAt);
    }
}
