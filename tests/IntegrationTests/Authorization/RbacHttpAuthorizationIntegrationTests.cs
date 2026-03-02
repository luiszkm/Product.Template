using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Authorization;

public class RbacHttpAuthorizationIntegrationTests : IClassFixture<RbacWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RbacHttpAuthorizationIntegrationTests(RbacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListUsers_ShouldReturn401_WhenNoTokenIsProvided()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_ShouldReturn403_WhenUserHasNoUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_ShouldReturn200_WhenUserHasUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.read");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }


    [Fact]
    public async Task DeleteUser_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/identity/{Guid.NewGuid()}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn404_WhenUserHasUsersManagePermissionAndTargetDoesNotExist()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/identity/{Guid.NewGuid()}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.manage");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{Guid.NewGuid()}/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ShouldReturn404_WhenUserHasUsersManagePermissionAndTargetDoesNotExist()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{Guid.NewGuid()}/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.manage");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task GetById_ShouldReturn403_WhenUserIsNotOwnerAndNotAdmin()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{ownerId}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");
        request.Headers.Add("X-Test-UserId", callerId.ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenUserIsOwner()
    {
        var userId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{userId}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");
        request.Headers.Add("X-Test-UserId", userId.ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}

public class RbacWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });
        });
    }
}
