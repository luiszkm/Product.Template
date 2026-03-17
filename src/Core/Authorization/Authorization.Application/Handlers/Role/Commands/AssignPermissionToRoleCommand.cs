using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

public record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : ICommand;
