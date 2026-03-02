using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Product.Template.Api.Controllers.v1;
using Product.Template.Api.Configurations;

namespace IntegrationTests.Authorization;

public class RbacEndpointAuthorizationIntegrationTests
{
    [Fact]
    public void IdentityRoleManagementEndpoints_ShouldRequireUsersManagePolicy()
    {
        var controller = typeof(IdentityController);
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "GetUserRoles" or "AddUserRole" or "RemoveUserRole" or "DeleteUser")
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(authorize);
            Assert.Equal(SecurityConfiguration.UsersManagePolicy, authorize!.Policy);
        }
    }

    [Fact]
    public void IdentityProtectedUserEndpoints_ShouldUseUserOnlyPolicy()
    {
        var controller = typeof(IdentityController);
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "GetById" or "UpdateUser")
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(authorize);
            Assert.Equal(SecurityConfiguration.UserOnlyPolicy, authorize!.Policy);
        }
    }

    [Fact]
    public void IdentityListUsersEndpoint_ShouldRequireUsersReadPolicy()
    {
        var controller = typeof(IdentityController);
        var method = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(m => m.Name == "ListUsers");

        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal(SecurityConfiguration.UsersReadPolicy, authorize!.Policy);
    }
}
