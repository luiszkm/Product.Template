using System.Reflection;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace ArchitectureTests;

public class TenancyInvariantTests
{
    private static readonly Assembly[] TenantAssemblies =
    [
        typeof(IMultiTenantEntity).Assembly,
        typeof(User).Assembly
    ];

    [Fact]
    public void MultiTenantEntities_ShouldExposeTenantIdWithNonPublicSetter()
    {
        var multiTenantTypes = TenantAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMultiTenantEntity).IsAssignableFrom(t))
            .Distinct()
            .ToList();

        var violations = new List<string>();

        foreach (var type in multiTenantTypes)
        {
            var tenantIdProperty = type.GetProperty(nameof(IMultiTenantEntity.TenantId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (tenantIdProperty is null || tenantIdProperty.PropertyType != typeof(long))
            {
                violations.Add($"{type.FullName} must declare a TenantId property of type long.");
                continue;
            }

            var setter = tenantIdProperty.GetSetMethod(nonPublic: true);
            if (setter is null)
            {
                violations.Add($"{type.FullName} must expose a setter (private/protected) for TenantId to allow assignment by interceptors.");
                continue;
            }

            if (setter.IsPublic || setter.IsFamily || setter.IsFamilyOrAssembly)
            {
                violations.Add($"{type.FullName} must not expose a public or protected setter for TenantId.");
            }
        }

        Assert.True(violations.Count == 0,
            $"Tenant invariants violated:\n - {string.Join("\n - ", violations)}");
    }
}

