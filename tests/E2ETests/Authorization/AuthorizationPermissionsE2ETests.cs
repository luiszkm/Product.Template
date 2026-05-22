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
public class AuthorizationPermissionsE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public AuthorizationPermissionsE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListPermissions_ShouldFilterBySearchTerm_WhenQueryProvided()
    {
        await SeedPermissionsAsync(
            ("e2e.perm.alpha", "Alpha permission"),
            ("e2e.perm.beta", "Beta permission"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e.perm.alpha&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedPermissionsResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal("e2e.perm.alpha", body.Data[0].Name);
    }

    [Fact]
    public async Task ListPermissions_ShouldSortByNameAscending_WhenSortParametersProvided()
    {
        await SeedPermissionsAsync(
            ("e2e.perm.sort.zulu", "Zulu"),
            ("e2e.perm.sort.alpha", "Alpha"));

        using var request = CreateAuthorizedListRequest("?searchTerm=e2e.perm.sort.&sortBy=name&sortDirection=asc&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedPermissionsResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal("e2e.perm.sort.alpha", body.Data[0].Name);
        Assert.Equal("e2e.perm.sort.zulu", body.Data[1].Name);
    }

    private HttpRequestMessage CreateAuthorizedListRequest(string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/authorization/permissions{query}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", AuthorizationPermissions.PermissionRead);
        return request;
    }

    private async Task SeedPermissionsAsync(params (string Name, string Description)[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(new TenantConfig
        {
            TenantId = 1,
            TenantKey = "public",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var (name, description) in permissions)
        {
            if (await db.Set<Permission>().AnyAsync(p => p.Name == name))
                continue;

            db.Set<Permission>().Add(Permission.Create(1L, name, description));
            await db.SaveChangesAsync();
        }
    }

    private sealed record PaginatedPermissionsResponse(
        int PageNumber,
        int PageSize,
        int TotalCount,
        List<PermissionItemResponse> Data);

    private sealed record PermissionItemResponse(Guid Id, string Name, string Description);
}
