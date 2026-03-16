using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Security;

namespace E2ETests.Security;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !IsSupportedHeader(authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader)
            ? userIdHeader.ToString()
            : Guid.NewGuid().ToString();

        var rolesHeader = Request.Headers.TryGetValue("X-Test-Roles", out var roleValues)
            ? roleValues.ToString()
            : string.Empty;
        var permissionsHeader = Request.Headers.TryGetValue("X-Test-Permissions", out var permissionValues)
            ? permissionValues.ToString()
            : string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "integration-test-user")
        };

        var roles = rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var permissions = permissionsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        claims.AddRange(permissions.Select(permission => new Claim(AuthorizationClaimTypes.Permission, permission)));

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        Logger.LogInformation("TestAuthHandler succeeded for user {UserId}", userId);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool IsSupportedHeader(string authHeader)
    {
        return authHeader.StartsWith("Test ", StringComparison.OrdinalIgnoreCase);
    }
}

