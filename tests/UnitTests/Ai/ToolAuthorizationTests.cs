using System.Security.Claims;
using Product.Template.Core.Ai.Application.Agent.Tools;
using Product.Template.Kernel.Application.Security;

namespace UnitTests.Ai;

public class ToolAuthorizationTests
{
    private const string Permission = "tenants:read";

    [Fact]
    public void EnsurePermission_ShouldNotThrow_WhenUserHasMatchingPermissionClaim()
    {
        var currentUser = new FakeCurrentUserService([new Claim(AuthorizationClaimTypes.Permission, Permission)]);

        var exception = Record.Exception(() => ToolAuthorization.EnsurePermission(currentUser, Permission));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsurePermission_ShouldNotThrow_WhenUserIsAdmin_RegardlessOfPermissionClaims()
    {
        var currentUser = new FakeCurrentUserService([new Claim(ClaimTypes.Role, "Admin")]);

        var exception = Record.Exception(() => ToolAuthorization.EnsurePermission(currentUser, Permission));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsurePermission_ShouldThrow_WhenUserLacksPermissionAndIsNotAdmin()
    {
        var currentUser = new FakeCurrentUserService([new Claim(AuthorizationClaimTypes.Permission, "other:permission")]);

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            ToolAuthorization.EnsurePermission(currentUser, Permission));

        Assert.Contains(Permission, exception.Message);
    }

    [Fact]
    public void EnsurePermission_ShouldThrow_WhenUserHasNoClaimsAtAll()
    {
        var currentUser = new FakeCurrentUserService([]);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ToolAuthorization.EnsurePermission(currentUser, Permission));
    }

    [Fact]
    public void EnsurePermission_ShouldThrow_WhenRoleClaimIsNonAdmin()
    {
        var currentUser = new FakeCurrentUserService([new Claim(ClaimTypes.Role, "Member")]);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ToolAuthorization.EnsurePermission(currentUser, Permission));
    }

    private sealed class FakeCurrentUserService(IEnumerable<Claim> claims) : ICurrentUserService
    {
        public Guid? UserId => Guid.NewGuid();
        public string? Email => "user@example.com";
        public string? UserName => "user";
        public bool IsAuthenticated => true;
        public IEnumerable<Claim> Claims { get; } = claims;
    }
}
