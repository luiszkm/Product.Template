using Product.Template.Core.Identity.Application.Queries.Role;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.Role.Commands;

public record CreateRoleCommand(string Name, string Description) : ICommand<RoleOutput>;
