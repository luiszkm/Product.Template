using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;

public record CreatePermissionCommand(string Name, string Description) : ICommand<PermissionOutput>;
