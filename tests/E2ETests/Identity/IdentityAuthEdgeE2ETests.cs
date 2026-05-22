using System.Net;
using System.Net.Http.Json;
using System.Text;
using E2ETests.Common;
using E2ETests.Security;

namespace E2ETests.Identity;

[Collection(RbacE2ECollection.Name)]
public class IdentityAuthEdgeE2ETests
{
    private readonly HttpClient _client;

    public IdentityAuthEdgeE2ETests(RbacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenTokenInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/identity/refresh", new
        {
            refreshToken = "invalid-garbage-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturn400_WhenBodyMissing()
    {
        var response = await _client.PostAsync(
            "/api/v1/identity/refresh",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenEmailInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email = "not-an-email",
            password = "Pass@123",
            firstName = "Auth",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
