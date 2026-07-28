using Product.Template.Core.Authorization.Application.Mappers;
using Product.Template.Core.Authorization.Domain.Entities;

namespace UnitTests.Mappers;

public class RoleMapperTests
{
    [Fact]
    public void ToOutput_ShouldMapAllFields()
    {
        var role = Role.Create(Guid.NewGuid(), "Admin", "Administrator role");

        var output = role.ToOutput();

        Assert.Equal(role.Id, output.Id);
        Assert.Equal("Admin", output.Name);
        Assert.Equal("Administrator role", output.Description);
    }

    [Fact]
    public void ToOutput_ShouldThrow_WhenRoleIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((Role)null!).ToOutput());
    }

    [Fact]
    public void ToOutputWithPermissions_ShouldReturnEmptyPermissions_WhenNoneAssigned()
    {
        var role = Role.Create(Guid.NewGuid(), "Admin", "Administrator role");

        var output = role.ToOutputWithPermissions();

        Assert.Empty(output.Permissions);
    }

    [Fact]
    public void ToOutputWithPermissions_ShouldFilterOutUnloadedPermissionNavigation()
    {
        var role = Role.Create(Guid.NewGuid(), "Admin", "Administrator role");
        role.AssignPermission(Guid.NewGuid());

        // RolePermission.Permission is a lazily-loaded EF navigation; when not
        // eagerly included it stays null, and the mapper must skip those rows.
        var output = role.ToOutputWithPermissions();

        Assert.Empty(output.Permissions);
    }

    [Fact]
    public void ToOutputList_ShouldMapEachRole()
    {
        var tenantId = Guid.NewGuid();
        var roles = new[]
        {
            Role.Create(tenantId, "Admin", "Admin role"),
            Role.Create(tenantId, "Viewer", "Viewer role")
        };

        var outputs = roles.ToOutputList().ToList();

        Assert.Equal(2, outputs.Count);
        Assert.Contains(outputs, o => o.Name == "Admin");
        Assert.Contains(outputs, o => o.Name == "Viewer");
    }

    [Fact]
    public void ToOutputList_ShouldThrow_WhenRolesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Role>)null!).ToOutputList());
    }
}
