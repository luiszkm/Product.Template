using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Authorization.Application.Handlers.Role.Commands;

public record CreateRoleCommand(string Name, string Description) : ICommand<RoleOutput>;
