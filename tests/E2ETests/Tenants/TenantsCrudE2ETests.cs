using System.Net;
using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Tenants.Application.Permissions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;

namespace E2ETests.Tenants;

[Collection(RbacE2ECollection.Name)]
public class TenantsCrudE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public TenantsCrudE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task CreateTenant_ShouldReturn201_WhenRequestIsValid()
    {
        var tenantKey = $"e2e-create-{Guid.NewGuid():N}";

        using var request = CreateManageRequest(HttpMethod.Post, "/api/v1/tenants");
        request.Content = JsonContent.Create(new
        {
            tenantKey,
            displayName = "E2E Create Corp",
            contactEmail = "create@test.com",
            isolationMode = TenantIsolationMode.SharedDb
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TenantItemResponse>();
        Assert.NotNull(body);
        Assert.Equal(tenantKey, body.TenantKey);
        Assert.Equal("E2E Create Corp", body.DisplayName);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task UpdateTenant_ShouldReturn200_WhenTenantExists()
    {
        var tenantId = await SeedTenantAsync($"e2e-update-{Guid.NewGuid():N}", "Before Update", "before@test.com");

        using var request = CreateManageRequest(HttpMethod.Put, $"/api/v1/tenants/{tenantId}");
        request.Content = JsonContent.Create(new
        {
            tenantId,
            displayName = "After Update",
            contactEmail = "after@test.com"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TenantItemResponse>();
        Assert.NotNull(body);
        Assert.Equal("After Update", body.DisplayName);
        Assert.Equal("after@test.com", body.ContactEmail);
    }

    [Fact]
    public async Task DeactivateTenant_ShouldReturn204_WhenTenantExists()
    {
        var tenantId = await SeedTenantAsync($"e2e-delete-{Guid.NewGuid():N}", "To Deactivate", null);

        using var request = CreateManageRequest(HttpMethod.Delete, $"/api/v1/tenants/{tenantId}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var getRequest = CreateReadRequest(HttpMethod.Get, $"/api/v1/tenants/{tenantId}");
        var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadFromJsonAsync<TenantItemResponse>();
        Assert.NotNull(body);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task GetTenantById_ShouldReturn404_WhenTenantDoesNotExist()
    {
        using var request = CreateReadRequest(HttpMethod.Get, $"/api/v1/tenants/{Guid.NewGuid()}");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpRequestMessage CreateReadRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", TenantsPermissions.Read);
        return request;
    }

    private HttpRequestMessage CreateManageRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", $"{TenantsPermissions.Read},{TenantsPermissions.Manage}");
        return request;
    }

    private async Task<Guid> SeedTenantAsync(string key, string displayName, string? email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        var existing = await db.Tenants.FirstOrDefaultAsync(t => t.TenantKey == key);
        if (existing is not null)
            return existing.TenantId;

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantConfig
        {
            TenantId = tenantId,
            TenantKey = key,
            DisplayName = displayName,
            ContactEmail = email,
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return tenantId;
    }

    private sealed record TenantItemResponse(
        Guid TenantId,
        string TenantKey,
        string DisplayName,
        string? ContactEmail,
        bool IsActive);
}
