using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

public record RevokePermissionFromRoleCommand(Guid RoleId, Guid PermissionId) : ICommand;
