using System.Security.Claims;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Ai.Application.Agent.Tools;

internal static class ToolAuthorization
{
    public static void EnsurePermission(ICurrentUserService currentUser, string permissionCode)
    {
        var isAdmin = currentUser.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var hasPermission = currentUser.Claims.Any(c =>
            c.Type == AuthorizationClaimTypes.Permission && c.Value == permissionCode);

        if (!isAdmin && !hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"Current user lacks permission '{permissionCode}' required by this tool.");
        }
    }
}
