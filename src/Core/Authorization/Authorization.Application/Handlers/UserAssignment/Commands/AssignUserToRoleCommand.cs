using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;

public record AssignUserToRoleCommand(Guid UserId, Guid RoleId) : ICommand;
