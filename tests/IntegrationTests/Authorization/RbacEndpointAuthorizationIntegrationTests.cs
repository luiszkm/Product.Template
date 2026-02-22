using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Product.Template.Api.Controllers.v1;

namespace IntegrationTests.Authorization;

public class RbacEndpointAuthorizationIntegrationTests
{
    [Fact]
    public void IdentityRoleManagementEndpoints_ShouldRequireAdminOnlyPolicy()
    {
        var controller = typeof(IdentityController);
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "GetUserRoles" or "AddUserRole" or "RemoveUserRole")
            .ToList();

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.NotNull(authorize);
            Assert.Equal(Product.Template.Api.Configurations.SecurityConfiguration.AdminOnlyPolicy, authorize!.Policy);
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
            Assert.Equal(Product.Template.Api.Configurations.SecurityConfiguration.UserOnlyPolicy, authorize!.Policy);
        }
    }
}
