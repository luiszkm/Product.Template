using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Product.Template.Api.GlobalFilter.Exceptions;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.Exceptions;

namespace UnitTests.Filters;

public class ApiGlobalExceptionFilterTests
{
    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "UnitTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ExceptionContext CreateContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
        return context;
    }

    private static void Run(ExceptionContext context, string? environmentName = null) =>
        new ApiGlobalExceptionFilter(new FakeHostEnvironment(environmentName ?? Environments.Production)).OnException(context);

    [Fact]
    public void OnException_ShouldReturn404_ForNotFoundException()
    {
        var context = CreateContext(new NotFoundException("User", Guid.NewGuid()));

        Run(context);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
        Assert.Equal(StatusCodes.Status404NotFound, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public void OnException_ShouldReturn422_ForDomainException()
    {
        var context = CreateContext(new DomainException("invalid state"));

        Run(context);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status);
    }

    [Fact]
    public void OnException_ShouldReturn400_ForBusinessRuleException()
    {
        var context = CreateContext(new BusinessRuleException("rule violated"));

        Run(context);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
    }

    [Fact]
    public void OnException_ShouldReturn400WithFieldErrors_ForValidationException()
    {
        var failures = new[] { new ValidationFailure("Email", "Email is required") };
        var context = CreateContext(new ValidationException(failures));

        Run(context);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        var errors = Assert.IsType<Dictionary<string, string[]>>(details.Extensions["errors"]);
        Assert.Contains("Email", errors.Keys);
    }

    [Fact]
    public void OnException_ShouldReturn401_ForUnauthorizedAccessException()
    {
        var context = CreateContext(new UnauthorizedAccessException("denied"));

        Run(context);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, details.Status);
    }

    [Fact]
    public void OnException_ShouldReturn500WithGenericMessage_InProduction_ForUnknownException()
    {
        var context = CreateContext(new InvalidOperationException("boom, contains secrets"));

        Run(context, Environments.Production);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, details.Status);
        Assert.Equal("An unexpected error occurred.", details.Detail);
        Assert.False(details.Extensions.ContainsKey("StackTrace"));
    }

    [Fact]
    public void OnException_ShouldExposeMessageAndStackTrace_InDevelopment_ForUnknownException()
    {
        var context = CreateContext(new InvalidOperationException("boom"));

        Run(context, Environments.Development);

        var details = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
        Assert.Equal("boom", details.Detail);
        Assert.True(details.Extensions.ContainsKey("StackTrace"));
    }
}
