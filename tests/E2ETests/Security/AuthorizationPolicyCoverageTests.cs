using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Controllers.v1;

namespace E2ETests.Security;

public class AuthorizationPolicyCoverageTests
{
    [Fact]
    public void ProtectedActions_ShouldUseExplicitPolicy_WhenAuthorizeIsPresent()
    {
        var controllerTypes = typeof(IdentityController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

        var failures = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var controllerAllowAnonymous = controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

            foreach (var method in controller.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName))
            {
                var hasAllowAnonymous = controllerAllowAnonymous || method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
                if (hasAllowAnonymous)
                    continue;

                var authorizeAttributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                    .Cast<AuthorizeAttribute>()
                    .ToList();

                if (authorizeAttributes.Count == 0)
                    continue;

                if (authorizeAttributes.Any(a => string.IsNullOrWhiteSpace(a.Policy)))
                {
                    failures.Add($"{controller.Name}.{method.Name} uses [Authorize] without explicit Policy.");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}

