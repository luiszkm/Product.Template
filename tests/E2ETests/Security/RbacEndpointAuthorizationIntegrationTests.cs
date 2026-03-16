using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Product.Template.Api.Configurations;
using Product.Template.Api.Controllers.v1;

namespace E2ETests.Security;

public class RbacEndpointAuthorizationIntegrationTests
{
    [Fact]
    public void IdentityRoleManagementEndpoints_ShouldRequireUsersManagePolicy()
    {
        var controller = typeof(IdentityController);
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "GetUserRoles" or "AddUserRole" or "RemoveUserRole" or "DeleteUser" or "CreateRole" or "UpdateRole" or "DeleteRole")
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
        var getById = controller.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
        var updateUser = controller.GetMethod("UpdateUser", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(getById);
        Assert.NotNull(updateUser);

        var getByIdAuthorize = getById!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        var updateAuthorize = updateUser!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(getByIdAuthorize);
        Assert.Equal(SecurityConfiguration.UserReadOrSelfPolicy, getByIdAuthorize!.Policy);

        Assert.NotNull(updateAuthorize);
        Assert.Equal(SecurityConfiguration.UserManageOrSelfPolicy, updateAuthorize!.Policy);
    }

    [Fact]
    public void IdentityUsersReadEndpoints_ShouldRequireUsersReadPolicy()
    {
        var controller = typeof(IdentityController);
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "ListUsers" or "ListRoles" or "GetRoleById")
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(authorize);
            Assert.Equal(SecurityConfiguration.UsersReadPolicy, authorize!.Policy);
        }
    }
}

