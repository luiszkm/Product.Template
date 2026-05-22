using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Product.Template.Api.Attributes;
using Product.Template.Api.Controllers.v1;
using Product.Template.Api.Filters;

namespace UnitTests.Filters;

public class FeatureGateActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_ShouldInvokeNext_WhenNoFeatureGateAttribute()
    {
        var featureManager = new FakeFeatureManager(enabled: true);
        var filter = new FeatureGateActionFilter(featureManager);
        var invoked = false;

        var context = CreateContext(typeof(IdentityController), nameof(IdentityController.GetById));
        var delegateResult = new ActionExecutedContext(context, [], null!)
        {
            Result = new OkResult()
        };

        await filter.OnActionExecutionAsync(context, () =>
        {
            invoked = true;
            return Task.FromResult(delegateResult);
        });

        Assert.True(invoked);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldReturn404_WhenFeatureIsDisabled()
    {
        var featureManager = new FakeFeatureManager(enabled: false);
        var filter = new FeatureGateActionFilter(featureManager);
        var invoked = false;

        var context = CreateContext(typeof(AiController), nameof(AiController.Chat));

        await filter.OnActionExecutionAsync(context, () =>
        {
            invoked = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.False(invoked);
        var notFound = Assert.IsType<NotFoundObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldInvokeNext_WhenFeatureIsEnabled()
    {
        var featureManager = new FakeFeatureManager(enabled: true);
        var filter = new FeatureGateActionFilter(featureManager);
        var invoked = false;

        var context = CreateContext(typeof(AiController), nameof(AiController.Chat));

        await filter.OnActionExecutionAsync(context, () =>
        {
            invoked = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.True(invoked);
    }

    private static ActionExecutingContext CreateContext(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName)!;
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = method,
            ControllerName = controllerType.Name,
            ActionName = methodName
        };

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: null!);
    }

    private sealed class FakeFeatureManager : IFeatureManager
    {
        private readonly bool _enabled;

        public FakeFeatureManager(bool enabled) => _enabled = enabled;

        public IAsyncEnumerable<string> GetFeatureNamesAsync() =>
            AsyncEnumerable.Empty<string>();

        public Task<bool> IsEnabledAsync(string feature) =>
            Task.FromResult(_enabled);

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) =>
            Task.FromResult(_enabled);
    }
}
