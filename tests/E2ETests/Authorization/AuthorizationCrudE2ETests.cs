using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Authorization;

[Collection(RbacE2ECollection.Name)]
public class AuthorizationCrudE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public AuthorizationCrudE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task AuthorizationCrud_ShouldCompleteFullWorkflow_WhenCallerHasRequiredPermissions()
    {
        var suffix = "a" + Guid.NewGuid().ToString("N")[..7];
        var roleName = $"e2e-crud-role-{suffix}";
        var updatedRoleName = $"e2e-crud-role-upd-{suffix}";
        var permissionName = $"authorization.e2e.{suffix}";
        var updatedPermissionName = $"authorization.e2e.upd{suffix}";
        var userEmail = $"e2e-crud-user-{suffix}@test.com";

        using var createRoleRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/authorization/roles",
            AuthorizationPermissions.RoleManage,
            new { Name = roleName, Description = "E2E CRUD role" });
        var createRoleResponse = await _client.SendAsync(createRoleRequest);
        Assert.Equal(System.Net.HttpStatusCode.Created, createRoleResponse.StatusCode);
        var createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(createdRole);

        using var getRoleRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/authorization/roles/{createdRole.Id}",
            AuthorizationPermissions.RoleRead);
        var getRoleResponse = await _client.SendAsync(getRoleRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, getRoleResponse.StatusCode);
        var fetchedRole = await getRoleResponse.Content.ReadFromJsonAsync<RoleWithPermissionsResponse>();
        Assert.NotNull(fetchedRole);
        Assert.Equal(roleName, fetchedRole.Name);

        using var updateRoleRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/authorization/roles/{createdRole.Id}",
            AuthorizationPermissions.RoleManage,
            new { RoleId = createdRole.Id, Name = updatedRoleName, Description = "Updated role" });
        var updateRoleResponse = await _client.SendAsync(updateRoleRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, updateRoleResponse.StatusCode);
        var updatedRole = await updateRoleResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(updatedRole);
        Assert.Equal(updatedRoleName, updatedRole.Name);

        using var createPermissionRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/authorization/permissions",
            AuthorizationPermissions.PermissionManage,
            new { Name = permissionName, Description = "E2E CRUD permission" });
        var createPermissionResponse = await _client.SendAsync(createPermissionRequest);
        Assert.Equal(System.Net.HttpStatusCode.Created, createPermissionResponse.StatusCode);
        var createdPermission = await createPermissionResponse.Content.ReadFromJsonAsync<PermissionResponse>();
        Assert.NotNull(createdPermission);

        using var updatePermissionRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/v1/authorization/permissions/{createdPermission.Id}",
            AuthorizationPermissions.PermissionManage,
            new { PermissionId = createdPermission.Id, Name = updatedPermissionName, Description = "Updated permission" });
        var updatePermissionResponse = await _client.SendAsync(updatePermissionRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, updatePermissionResponse.StatusCode);
        var updatedPermission = await updatePermissionResponse.Content.ReadFromJsonAsync<PermissionResponse>();
        Assert.NotNull(updatedPermission);
        Assert.Equal(updatedPermissionName, updatedPermission.Name);

        using var assignPermissionRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/authorization/roles/{createdRole.Id}/permissions",
            AuthorizationPermissions.RoleManage,
            new { PermissionId = createdPermission.Id });
        var assignPermissionResponse = await _client.SendAsync(assignPermissionRequest);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, assignPermissionResponse.StatusCode);

        using var getRolePermissionsRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/authorization/roles/{createdRole.Id}/permissions",
            AuthorizationPermissions.RoleRead);
        var getRolePermissionsResponse = await _client.SendAsync(getRolePermissionsRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, getRolePermissionsResponse.StatusCode);
        var roleWithPermissions = await getRolePermissionsResponse.Content.ReadFromJsonAsync<RoleWithPermissionsResponse>();
        Assert.NotNull(roleWithPermissions);
        Assert.Contains(roleWithPermissions.Permissions, p => p.Id == createdPermission.Id);

        using var revokePermissionRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/authorization/roles/{createdRole.Id}/permissions/{createdPermission.Id}",
            AuthorizationPermissions.RoleManage);
        var revokePermissionResponse = await _client.SendAsync(revokePermissionRequest);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, revokePermissionResponse.StatusCode);

        var userId = await SeedUserAsync(userEmail, "E2E", "User");

        using var assignUserRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/authorization/users/{userId}/roles",
            AuthorizationPermissions.RoleManage,
            new { RoleId = createdRole.Id });
        var assignUserResponse = await _client.SendAsync(assignUserRequest);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, assignUserResponse.StatusCode);

        using var getUserRolesRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/authorization/users/{userId}/roles",
            AuthorizationPermissions.RoleRead);
        var getUserRolesResponse = await _client.SendAsync(getUserRolesRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, getUserRolesResponse.StatusCode);
        var userRoles = await getUserRolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();
        Assert.NotNull(userRoles);
        Assert.Contains(userRoles, r => r.Id == createdRole.Id);

        using var revokeUserRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/authorization/users/{userId}/roles/{createdRole.Id}",
            AuthorizationPermissions.RoleManage);
        var revokeUserResponse = await _client.SendAsync(revokeUserRequest);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, revokeUserResponse.StatusCode);

        using var verifyUserRolesRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/authorization/users/{userId}/roles",
            AuthorizationPermissions.RoleRead);
        var verifyUserRolesResponse = await _client.SendAsync(verifyUserRolesRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, verifyUserRolesResponse.StatusCode);
        var remainingRoles = await verifyUserRolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();
        Assert.NotNull(remainingRoles);
        Assert.DoesNotContain(remainingRoles, r => r.Id == createdRole.Id);
    }

    private HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string permission,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", permission);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    private async Task<Guid> SeedUserAsync(string email, string firstName, string lastName)
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
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        if (existing is not null)
            return existing.Id;

        var user = User.Create(WellKnownTenants.Public, email, "hashed:test", firstName, lastName);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private sealed record RoleResponse(Guid Id, string Name, string Description, DateTime CreatedAt);

    private sealed record PermissionResponse(Guid Id, string Name, string Description, DateTime CreatedAt);

    private sealed record RoleWithPermissionsResponse(
        Guid Id,
        string Name,
        string Description,
        DateTime CreatedAt,
        List<PermissionResponse> Permissions);
}
