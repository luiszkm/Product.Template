using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Identity.Application.Permissions;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Identity;

[Collection(RbacE2ECollection.Name)]
public class IdentityUsersE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public IdentityUsersE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListUsers_ShouldFilterBySearchTerm_WhenQueryProvided()
    {
        await SeedUsersAsync(
            ("filter-alpha@test.com", "Alpha", "One"),
            ("filter-beta@test.com", "Beta", "Two"));

        using var request = CreateAuthorizedListRequest("?searchTerm=filter-alpha&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal("filter-alpha@test.com", body.Data[0].Email);
    }

    [Fact]
    public async Task ListUsers_ShouldPaginate_WhenPageParametersProvided()
    {
        await SeedUsersAsync(
            ("page-a@test.com", "Page", "A"),
            ("page-b@test.com", "Page", "B"),
            ("page-c@test.com", "Page", "C"));

        using var request = CreateAuthorizedListRequest("?searchTerm=page-&pageNumber=2&pageSize=1&sortBy=email&sortDirection=asc");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body.TotalCount);
        Assert.Single(body.Data);
        Assert.Equal(2, body.PageNumber);
        Assert.Equal(1, body.PageSize);
        Assert.Equal("page-b@test.com", body.Data[0].Email);
    }

    [Fact]
    public async Task ListUsers_ShouldSortByEmailDescending_WhenSortParametersProvided()
    {
        await SeedUsersAsync(
            ("sort-a@test.com", "Sort", "A"),
            ("sort-z@test.com", "Sort", "Z"));

        using var request = CreateAuthorizedListRequest("?searchTerm=sort-&sortBy=email&sortDirection=desc&pageSize=50");
        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal("sort-z@test.com", body.Data[0].Email);
        Assert.Equal("sort-a@test.com", body.Data[1].Email);
    }

    private HttpRequestMessage CreateAuthorizedListRequest(string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity{query}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", IdentityPermissions.UserRead);
        return request;
    }

    private async Task SeedUsersAsync(params (string Email, string FirstName, string LastName)[] users)
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
        foreach (var (email, firstName, lastName) in users)
        {
            if (await db.Users.AnyAsync(u => u.Email.Value == email))
                continue;

            var user = User.Create(WellKnownTenants.Public, email, "hashed:test", firstName, lastName);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }

    private sealed record PaginatedUsersResponse(
        int PageNumber,
        int PageSize,
        int TotalCount,
        List<UserItemResponse> Data);

    private sealed record UserItemResponse(
        Guid Id,
        string Email,
        string FirstName,
        string LastName);
}
