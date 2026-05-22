using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Authorization;

[Collection(RbacE2ECollection.Name)]
public class AuthorizationRolesE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public AuthorizationRolesE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListRoles_ShouldFilterBySearchTerm_WhenQueryProvided()
    {
        await SeedRolesAsync(
            ("e2e-filter-alpha", "Alpha role"),
            ("e2e-filter-beta", "Beta role"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e-filter-alpha&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedRolesResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal("e2e-filter-alpha", body.Data[0].Name);
    }

    [Fact]
    public async Task ListRoles_ShouldPaginate_WhenPageParametersProvided()
    {
        await SeedRolesAsync(
            ("e2e-page-a", "Page A"),
            ("e2e-page-b", "Page B"),
            ("e2e-page-c", "Page C"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e-page-&pageNumber=2&pageSize=1&sortBy=name&sortDirection=asc");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedRolesResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal(2, body.PageNumber);
        Assert.Equal("e2e-page-b", body.Data[0].Name);
    }

    [Fact]
    public async Task ListRoles_ShouldSortByNameDescending_WhenSortParametersProvided()
    {
        await SeedRolesAsync(
            ("e2e-sort-a", "Sort A"),
            ("e2e-sort-z", "Sort Z"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e-sort-&sortBy=name&sortDirection=desc&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedRolesResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal("e2e-sort-z", body.Data[0].Name);
        Assert.Equal("e2e-sort-a", body.Data[1].Name);
    }

    private HttpRequestMessage CreateAuthorizedListRequest(string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/authorization/roles{query}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", AuthorizationPermissions.RoleRead);
        return request;
    }

    private async Task SeedRolesAsync(params (string Name, string Description)[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(new TenantConfig
        {
            TenantId = WellKnownTenants.Public,
            TenantKey = "public",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var (name, description) in roles)
        {
            if (await db.Set<Role>().AnyAsync(r => r.Name == name))
                continue;

            db.Set<Role>().Add(Role.Create(WellKnownTenants.Public, name, description));
            await db.SaveChangesAsync();
        }
    }

    private sealed record PaginatedRolesResponse(
        int PageNumber,
        int PageSize,
        int TotalCount,
        List<RoleItemResponse> Data);

    private sealed record RoleItemResponse(Guid Id, string Name, string Description);
}
