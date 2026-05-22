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
public class TenantsE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public TenantsE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListTenants_ShouldFilterBySearchTerm_WhenQueryProvided()
    {
        await SeedTenantsAsync(
            ("e2e-tenant-alpha", "Alpha Corp", "alpha@test.com"),
            ("e2e-tenant-beta", "Beta Corp", "beta@test.com"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e-tenant-alpha&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedTenantsResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal("e2e-tenant-alpha", body.Data[0].TenantKey);
    }

    [Fact]
    public async Task GetTenantById_ShouldReturn200_WhenTenantExists()
    {
        var tenantId = await SeedTenantAsync("e2e-get-one", "Get One Corp", null);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tenants/{tenantId}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", TenantsPermissions.Read);

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TenantItemResponse>();
        Assert.NotNull(body);
        Assert.Equal(tenantId, body.TenantId);
        Assert.Equal("e2e-get-one", body.TenantKey);
    }

    private HttpRequestMessage CreateAuthorizedListRequest(string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tenants{query}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", TenantsPermissions.Read);
        return request;
    }

    private async Task SeedTenantsAsync(params (string Key, string DisplayName, string? Email)[] tenants)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        foreach (var (key, displayName, email) in tenants)
        {
            if (await db.Tenants.AnyAsync(t => t.TenantKey == key))
                continue;

            db.Tenants.Add(new TenantConfig
            {
                TenantId = Guid.NewGuid(),
                TenantKey = key,
                DisplayName = displayName,
                ContactEmail = email,
                IsolationMode = TenantIsolationMode.SharedDb,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
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

    private sealed record PaginatedTenantsResponse(
        int PageNumber,
        int PageSize,
        int TotalCount,
        List<TenantItemResponse> Data);

    private sealed record TenantItemResponse(
        Guid TenantId,
        string TenantKey,
        string DisplayName);
}
