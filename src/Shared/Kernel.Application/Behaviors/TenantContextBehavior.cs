using MediatR;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Application.Behaviors;

/// <summary>
/// Pipeline behavior que bloqueia commands/queries quando o tenant não foi resolvido.
/// </summary>
public sealed class TenantContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantContextBehavior<TRequest, TResponse>> _logger;

    public TenantContextBehavior(
        ITenantContext tenantContext,
        ILogger<TenantContextBehavior<TRequest, TResponse>> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            _logger.LogWarning("Tentativa de execução sem tenant resolvido para {RequestType}", typeof(TRequest).Name);
            throw new BusinessRuleException("Tenant must be resolved before handling the request.");
        }

        return await next();
    }
}
