using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Identity.Application.Handlers.User.Commands;

public record RemoveUserRoleCommand(Guid UserId, string RoleName) : ICommand;
