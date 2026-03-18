using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Product.Template.Api.Configurations;

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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserOwnershipHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnershipRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return Task.CompletedTask;

        if (context.User.Identity?.IsAuthenticated != true)
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

        var currentUserIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? context.User.FindFirstValue("sub");

        if (Guid.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == targetUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
