using System.Reflection;
using NetArchTest.Rules;
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

namespace ArchitectureTests;

/// <summary>
/// Enforces naming conventions across the codebase.
/// </summary>
public class NamingConventionTests
{
    private static readonly Assembly[] AllSourceAssemblies =
    [
        typeof(Product.Template.Kernel.Domain.SeedWorks.Entity).Assembly,
        typeof(Product.Template.Kernel.Application.DependencyInjection).Assembly,
        typeof(Product.Template.Kernel.Infrastructure.Persistence.AppDbContext).Assembly,
        typeof(Product.Template.Core.Identity.Domain.Entities.User).Assembly,
        typeof(Product.Template.Core.Identity.Application.Handlers.Auth.LoginCommandHandler).Assembly,
        typeof(Product.Template.Core.Identity.Infrastructure.DependencyInjection).Assembly,
        typeof(Product.Template.Core.Authorization.Domain.Entities.Role).Assembly,
        typeof(Product.Template.Core.Authorization.Application.Permissions.AuthorizationPermissions).Assembly,
        typeof(Product.Template.Core.Authorization.Infrastructure.DependencyInjection).Assembly,
        typeof(Product.Template.Core.Tenants.Domain.Entities.Tenant).Assembly,
        typeof(Product.Template.Core.Tenants.Application.Permissions.TenantsPermissions).Assembly,
        typeof(Product.Template.Core.Tenants.Infrastructure.DependencyInjection).Assembly,
        typeof(Product.Template.Api.Configurations.SecurityConfiguration).Assembly,
    ];

    [Fact]
    public void CommandHandlers_ShouldEndWith_CommandHandler()
    {
        var result = Types.InAssemblies(AllSourceAssemblies)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Command handlers should end with 'CommandHandler'. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void QueryHandlers_ShouldEndWith_QueryHandler()
    {
        var result = Types.InAssemblies(AllSourceAssemblies)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Query handlers should end with 'QueryHandler'. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        var result = Types.InAssemblies(AllSourceAssemblies)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Interfaces should start with 'I'. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void Entities_ShouldInheritFrom_EntityBase()
    {
        var domainAssemblies = new[]
        {
            typeof(Product.Template.Core.Identity.Domain.Entities.User).Assembly,
            typeof(Product.Template.Core.Authorization.Domain.Entities.Role).Assembly,
            typeof(Product.Template.Core.Tenants.Domain.Entities.Tenant).Assembly,
        };

        var result = Types.InAssemblies(domainAssemblies)
            .That()
            .ResideInNamespaceContaining(".Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(Entity))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain entities should inherit from Entity. Violations: {FormatFailures(result)}");
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypes is null || !result.FailingTypes.Any())
            return "(none)";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}

