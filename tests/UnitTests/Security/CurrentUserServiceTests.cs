using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Kernel.Infrastructure.Security;

namespace UnitTests.Security;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_ShouldReturnNull_WhenNoHttpContext()
    {
        var sut = new CurrentUserService(new HttpContextAccessor());

        Assert.Null(sut.UserId);
        Assert.False(sut.IsAuthenticated);
        Assert.Null(sut.Email);
    }

    [Fact]
    public void Properties_ShouldReadFromClaimsPrincipal_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "user@acme.com"),
            new Claim(ClaimTypes.Name, "Jane Doe")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var sut = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(userId, sut.UserId);
        Assert.Equal("user@acme.com", sut.Email);
        Assert.Equal("Jane Doe", sut.UserName);
        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public void UserName_ShouldFallBackToEmail_WhenNameClaimIsMissing()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "fallback@acme.com") };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };

        var sut = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("fallback@acme.com", sut.UserName);
    }

    [Fact]
    public void UserId_ShouldReturnNull_WhenClaimIsNotAGuid()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };

        var sut = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

        Assert.Null(sut.UserId);
    }
}
