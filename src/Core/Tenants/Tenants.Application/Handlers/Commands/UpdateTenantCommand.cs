using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Handlers.Commands;

public record UpdateTenantCommand(long TenantId, string DisplayName, string? ContactEmail) : ICommand<TenantOutput>;
