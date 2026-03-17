using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Enforces that layer dependencies flow inward only:
/// Domain ← Application ← Infrastructure ← Api
/// </summary>
public class LayerDependencyTests
{
    // Assembly references
    private static readonly Assembly KernelDomainAssembly = typeof(Product.Template.Kernel.Domain.SeedWorks.Entity).Assembly;
    private static readonly Assembly KernelApplicationAssembly = typeof(Product.Template.Kernel.Application.DependencyInjection).Assembly;
    private static readonly Assembly KernelInfrastructureAssembly = typeof(Product.Template.Kernel.Infrastructure.Persistence.AppDbContext).Assembly;
    private static readonly Assembly IdentityDomainAssembly = typeof(Product.Template.Core.Identity.Domain.Entities.User).Assembly;
    private static readonly Assembly IdentityApplicationAssembly = typeof(Product.Template.Core.Identity.Application.Handlers.Auth.LoginCommandHandler).Assembly;
    private static readonly Assembly IdentityInfrastructureAssembly = typeof(Product.Template.Core.Identity.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly AuthorizationDomainAssembly = typeof(Product.Template.Core.Authorization.Domain.Entities.Role).Assembly;
    private static readonly Assembly AuthorizationApplicationAssembly = typeof(Product.Template.Core.Authorization.Application.Permissions.AuthorizationPermissions).Assembly;
    private static readonly Assembly AuthorizationInfrastructureAssembly = typeof(Product.Template.Core.Authorization.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly TenantsDomainAssembly = typeof(Product.Template.Core.Tenants.Domain.Entities.Tenant).Assembly;
    private static readonly Assembly TenantsApplicationAssembly = typeof(Product.Template.Core.Tenants.Application.Permissions.TenantsPermissions).Assembly;
    private static readonly Assembly TenantsInfrastructureAssembly = typeof(Product.Template.Core.Tenants.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Product.Template.Api.Configurations.SecurityConfiguration).Assembly;

    // Namespace constants
    private const string KernelDomainNamespace = "Product.Template.Kernel.Domain";
    private const string KernelApplicationNamespace = "Product.Template.Kernel.Application";
    private const string KernelInfrastructureNamespace = "Product.Template.Kernel.Infrastructure";
    private const string IdentityDomainNamespace = "Product.Template.Core.Identity.Domain";
    private const string IdentityApplicationNamespace = "Product.Template.Core.Identity.Application";
    private const string IdentityInfrastructureNamespace = "Product.Template.Core.Identity.Infrastructure";
    private const string AuthorizationDomainNamespace = "Product.Template.Core.Authorization.Domain";
    private const string AuthorizationApplicationNamespace = "Product.Template.Core.Authorization.Application";
    private const string AuthorizationInfrastructureNamespace = "Product.Template.Core.Authorization.Infrastructure";
    private const string TenantsDomainNamespace = "Product.Template.Core.Tenants.Domain";
    private const string TenantsApplicationNamespace = "Product.Template.Core.Tenants.Application";
    private const string TenantsInfrastructureNamespace = "Product.Template.Core.Tenants.Infrastructure";
    private const string ApiNamespace = "Product.Template.Api";

    [Fact]
    public void KernelDomain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(KernelDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(KernelApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Domain should not depend on Kernel.Application. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void KernelDomain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(KernelDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(KernelInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Domain should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void KernelDomain_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(KernelDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Domain should not depend on Api. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void IdentityDomain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(IdentityDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(IdentityApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Identity.Domain should not depend on Identity.Application. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void IdentityDomain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(IdentityDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(IdentityInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Identity.Domain should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void KernelApplication_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(KernelApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(KernelInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Application should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void KernelApplication_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(KernelApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Application should not depend on Api. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void IdentityApplication_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(IdentityApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(IdentityInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Identity.Application should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void IdentityApplication_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(IdentityApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Identity.Application should not depend on Api. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(KernelInfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Kernel.Infrastructure should not depend on Api. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void AuthorizationDomain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(AuthorizationDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuthorizationApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Authorization.Domain should not depend on Authorization.Application. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void AuthorizationDomain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(AuthorizationDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuthorizationInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Authorization.Domain should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void AuthorizationApplication_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(AuthorizationApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuthorizationInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Authorization.Application should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void AuthorizationApplication_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(AuthorizationApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Authorization.Application should not depend on Api. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void TenantsDomain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(TenantsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(TenantsApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Tenants.Domain should not depend on Tenants.Application. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void TenantsDomain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(TenantsDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(TenantsInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Tenants.Domain should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void TenantsApplication_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(TenantsApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(TenantsInfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Tenants.Application should not depend on Infrastructure. Violations: {FormatFailures(result)}");
    }

    [Fact]
    public void TenantsApplication_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(TenantsApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Tenants.Application should not depend on Api. Violations: {FormatFailures(result)}");
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypes is null || !result.FailingTypes.Any())
            return "(none)";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
