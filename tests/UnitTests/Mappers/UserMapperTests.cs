using Product.Template.Core.Identity.Application.Mappers;
using Product.Template.Core.Identity.Domain.Entities;

namespace UnitTests.Mappers;

public class UserMapperTests
{
    [Fact]
    public void ToOutput_ShouldMapAllFields()
    {
        var user = User.Create(Guid.NewGuid(), "user@acme.com", "hash", "Jane", "Doe");

        var output = user.ToOutput();

        Assert.Equal(user.Id, output.Id);
        Assert.Equal("user@acme.com", output.Email);
        Assert.Equal("Jane", output.FirstName);
        Assert.Equal("Doe", output.LastName);
        Assert.False(output.EmailConfirmed);
    }

    [Fact]
    public void ToOutput_ShouldThrow_WhenUserIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((User)null!).ToOutput());
    }

    [Fact]
    public void ToOutputList_ShouldMapEachUser()
    {
        var users = new[]
        {
            User.Create(Guid.NewGuid(), "a@acme.com", "hash", "A", "One"),
            User.Create(Guid.NewGuid(), "b@acme.com", "hash", "B", "Two")
        };

        var outputs = users.ToOutputList().ToList();

        Assert.Equal(2, outputs.Count);
        Assert.Contains(outputs, o => o.Email == "a@acme.com");
        Assert.Contains(outputs, o => o.Email == "b@acme.com");
    }

    [Fact]
    public void ToOutputList_ShouldThrow_WhenUsersIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<User>)null!).ToOutputList());
    }
}
