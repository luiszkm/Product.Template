using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

public record DeleteRoleCommand(Guid RoleId) : ICommand;
