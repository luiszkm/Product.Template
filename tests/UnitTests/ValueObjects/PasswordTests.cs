using Product.Template.Core.Identity.Domain.ValueObjects;

namespace UnitTests.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void Create_ShouldSucceed_ForStrongPassword()
    {
        var password = Password.Create("Str0ng-Pass!");

        Assert.Equal("Str0ng-Pass!", password.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenEmpty(string input)
    {
        Assert.Throws<ArgumentException>(() => Password.Create(input));
    }

    [Fact]
    public void Create_ShouldThrow_WhenShorterThan8Characters()
    {
        Assert.Throws<ArgumentException>(() => Password.Create("Ab1!"));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMissingUppercase()
    {
        Assert.Throws<ArgumentException>(() => Password.Create("lowercase1!"));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMissingLowercase()
    {
        Assert.Throws<ArgumentException>(() => Password.Create("UPPERCASE1!"));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMissingDigit()
    {
        Assert.Throws<ArgumentException>(() => Password.Create("NoDigits!"));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMissingSpecialCharacter()
    {
        Assert.Throws<ArgumentException>(() => Password.Create("NoSpecial1"));
    }

    [Fact]
    public void ToString_ShouldMaskValue()
    {
        var password = Password.Create("Str0ng-Pass!");

        Assert.Equal("********", password.ToString());
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnUnderlyingValue()
    {
        var password = Password.Create("Str0ng-Pass!");

        string value = password;

        Assert.Equal("Str0ng-Pass!", value);
    }
}
