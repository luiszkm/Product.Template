using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Tenants.Application.Handlers.Commands;

public record CreateTenantCommand(
    string TenantKey,
    string DisplayName,
    string? ContactEmail,
    TenantIsolationMode IsolationMode
) : ICommand<TenantOutput>;
