using Product.Template.Kernel.Application.Security;

namespace UnitTests.Security;

public class PermissionCatalogTests
{
    [Fact]
    public void Register_ShouldMakePermissionDiscoverable_ByNormalizedCode()
    {
        var catalog = new PermissionCatalog();
        catalog.Register(new PermissionDescriptor("Users:Read", "identity", "user", "read", "Read users"));

        Assert.True(catalog.Contains("users:read"));
        Assert.True(catalog.Contains("  USERS:READ  "));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForUnregisteredCode()
    {
        var catalog = new PermissionCatalog();

        Assert.False(catalog.Contains("unknown:code"));
    }

    [Fact]
    public void TryGet_ShouldReturnDescriptor_WhenRegistered()
    {
        var catalog = new PermissionCatalog();
        var descriptor = new PermissionDescriptor("users:read", "identity", "user", "read", "Read users");
        catalog.Register(descriptor);

        var found = catalog.TryGet("users:read", out var result);

        Assert.True(found);
        Assert.Equal(descriptor, result);
    }

    [Fact]
    public void Register_ShouldOverwriteExistingDescriptor_ForSameNormalizedCode()
    {
        var catalog = new PermissionCatalog();
        catalog.Register(new PermissionDescriptor("users:read", "identity", "user", "read", "Old description"));
        catalog.Register(new PermissionDescriptor("USERS:READ", "identity", "user", "read", "New description"));

        catalog.TryGet("users:read", out var result);

        Assert.Equal("New description", result!.Description);
        Assert.Single(catalog.GetAll());
    }

    [Fact]
    public void Register_ShouldThrow_WhenCodeIsEmpty()
    {
        var catalog = new PermissionCatalog();

        Assert.Throws<ArgumentException>(() =>
            catalog.Register(new PermissionDescriptor("", "identity", "user", "read", "Read users")));
    }

    [Fact]
    public void GetAll_ShouldReturnEveryRegisteredPermission()
    {
        var catalog = new PermissionCatalog();
        catalog.Register(
            new PermissionDescriptor("users:read", "identity", "user", "read", "Read users"),
            new PermissionDescriptor("tenants:read", "tenants", "tenant", "read", "Read tenants"));

        Assert.Equal(2, catalog.GetAll().Count);
    }
}
