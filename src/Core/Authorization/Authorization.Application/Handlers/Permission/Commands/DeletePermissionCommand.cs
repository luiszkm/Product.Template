using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;

public record DeletePermissionCommand(Guid PermissionId) : ICommand;
