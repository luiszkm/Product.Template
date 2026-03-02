using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Role.Commands;

public record DeleteRoleCommand(Guid RoleId) : ICommand;
