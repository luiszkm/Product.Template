using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Domain.Entities;

namespace UnitTests.Mappers;

public class PermissionMapperTests
{
    [Fact]
    public void ToOutput_ShouldMapAllFields()
    {
        var permission = Permission.Create(Guid.NewGuid(), "users:read", "Read users");

        var output = permission.ToOutput();

        Assert.Equal(permission.Id, output.Id);
        Assert.Equal("users:read", output.Name);
        Assert.Equal("Read users", output.Description);
    }

    [Fact]
    public void ToOutput_ShouldThrow_WhenPermissionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((Permission)null!).ToOutput());
    }

    [Fact]
    public void ToOutputList_ShouldMapEachPermission()
    {
        var tenantId = Guid.NewGuid();
        var permissions = new[]
        {
            Permission.Create(tenantId, "users:read", "Read"),
            Permission.Create(tenantId, "users:write", "Write")
        };

        var outputs = permissions.ToOutputList().ToList();

        Assert.Equal(2, outputs.Count);
        Assert.Contains(outputs, o => o.Name == "users:read");
        Assert.Contains(outputs, o => o.Name == "users:write");
    }

    [Fact]
    public void ToOutputList_ShouldThrow_WhenPermissionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Permission>)null!).ToOutputList());
    }
}
