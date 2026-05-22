using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Product.Template.Core.Ai.Application.Handlers;

namespace E2ETests.Ai;

[Collection(RbacE2ECollection.Name)]
public class AiChatE2ETests
{
    private readonly HttpClient _client;

    public AiChatE2ETests(RbacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task Chat_ShouldReturn401_WhenNoTokenIsProvided()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/chat")
        {
            Content = JsonContent.Create(new { message = "Hello" })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_ShouldReturn200Or503_WhenAuthenticated()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/chat")
        {
            Content = JsonContent.Create(new { message = "Hello" })
        };
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");

        var response = await _client.SendAsync(request);

        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.OK
                or System.Net.HttpStatusCode.ServiceUnavailable
                or System.Net.HttpStatusCode.BadRequest,
            $"Unexpected status {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<ChatOutput>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body.Reply));
        }
    }
}
