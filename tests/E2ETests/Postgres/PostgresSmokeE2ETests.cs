using System.Net;
using E2ETests.Common;
using E2ETests.Security;
using Product.Template.Core.Identity.Application.Permissions;

namespace E2ETests.Postgres;

[Collection(PostgresCollection.Name)]
public class PostgresSmokeE2ETests
{
    private readonly PostgresE2EFixture _fixture;

    public PostgresSmokeE2ETests(PostgresE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListUsers_ShouldReturn200Or401_WhenAppStartsWithPostgres()
    {
        if (!_fixture.IsDockerAvailable || _fixture.Factory is null)
            return;

        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant", "public");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", IdentityPermissions.UserRead);

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized,
            $"Expected 200 or 401 but got {(int)response.StatusCode} {response.StatusCode}");
    }
}
