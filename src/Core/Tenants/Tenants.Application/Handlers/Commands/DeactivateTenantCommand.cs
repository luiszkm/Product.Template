using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Handlers.Commands;

public record DeactivateTenantCommand(long TenantId) : ICommand;
