using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.FeatureManagement;
using Product.Template.Api.Attributes;

namespace Product.Template.Api.Filters;

public sealed class FeatureGateActionFilter : IAsyncActionFilter
{
    private readonly IFeatureManager _featureManager;

    public FeatureGateActionFilter(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var featureName = ResolveFeatureName(context);
        if (featureName is null)
        {
            await next();
            return;
        }

        if (!await _featureManager.IsEnabledAsync(featureName))
        {
            context.Result = new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Feature disabled",
                Status = StatusCodes.Status404NotFound,
                Detail = $"The feature '{featureName}' is not enabled in this environment."
            });
            return;
        }

        await next();
    }

    private static string? ResolveFeatureName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return null;

        var methodAttribute = descriptor.MethodInfo.GetCustomAttributes(typeof(FeatureGateAttribute), inherit: true)
            .OfType<FeatureGateAttribute>()
            .FirstOrDefault();
        if (methodAttribute is not null)
            return methodAttribute.FeatureName;

        var typeAttribute = descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(FeatureGateAttribute), inherit: true)
            .OfType<FeatureGateAttribute>()
            .FirstOrDefault();

        return typeAttribute?.FeatureName;
    }
}
