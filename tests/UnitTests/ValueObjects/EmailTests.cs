using Product.Template.Core.Identity.Domain.ValueObjects;

namespace UnitTests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@acme.com")]
    [InlineData("USER@ACME.COM")]
    [InlineData("  user@acme.com  ")]
    public void Create_ShouldNormalizeToLowerCaseAndTrimmed(string input)
    {
        var email = Email.Create(input);

        Assert.Equal("user@acme.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Create_ShouldThrow_WhenEmailIsInvalid(string input)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(input));
    }

    [Fact]
    public void Create_ShouldThrow_WhenNull()
    {
        Assert.Throws<ArgumentException>(() => Email.Create(null!));
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnUnderlyingValue()
    {
        var email = Email.Create("user@acme.com");

        string value = email;

        Assert.Equal("user@acme.com", value);
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var a = Email.Create("user@acme.com");
        var b = Email.Create("USER@acme.com");

        Assert.Equal(a, b);
    }
}
