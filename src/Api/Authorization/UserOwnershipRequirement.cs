using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Product.Template.Api.Configurations;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Api.Authorization;

public sealed class UserOwnershipRequirement : IAuthorizationRequirement
{
    public UserOwnershipRequirement(string requiredPermission)
    {
        RequiredPermission = requiredPermission;
    }

    public string RequiredPermission { get; }
}

public sealed class UserOwnershipHandler : AuthorizationHandler<UserOwnershipRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserOwnershipHandler(ICurrentUserService currentUserService, IHttpContextAccessor httpContextAccessor)
    {
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnershipRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return Task.CompletedTask;

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
            return Task.CompletedTask;

        var targetRouteValue = httpContext.Request.RouteValues.GetValueOrDefault("id")?.ToString();
        if (!Guid.TryParse(targetRouteValue, out var targetUserId))
            return Task.CompletedTask;

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(SecurityConfiguration.PermissionClaimType, requirement.RequiredPermission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (_currentUserService.UserId == targetUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
