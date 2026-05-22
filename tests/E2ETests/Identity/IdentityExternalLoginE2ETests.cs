using System.Net;
using System.Net.Http.Json;
using System.Text;
using E2ETests.Common;
using E2ETests.Security;

namespace E2ETests.Identity;

[Collection(RbacE2ECollection.Name)]
public class IdentityExternalLoginE2ETests
{
    private readonly HttpClient _client;

    public IdentityExternalLoginE2ETests(RbacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ExternalLogin_ShouldReturn400_WhenProviderInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/identity/external-login", new
        {
            provider = new string('x', 101),
            code = "auth-code"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExternalLogin_ShouldReturn400_WhenRequiredFieldsMissing()
    {
        var response = await _client.PostAsync(
            "/api/v1/identity/external-login",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
