using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Tenants.Application.Queries;

public record GetTenantByIdQuery(long TenantId) : IQuery<TenantOutput>;
