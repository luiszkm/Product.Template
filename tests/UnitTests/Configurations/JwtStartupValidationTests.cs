using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Product.Template.Api.Configurations;

namespace UnitTests.Configurations;

public class JwtStartupValidationTests
{
    private const string ValidSecret = "a-strong-secret-that-is-at-least-32-characters-long";
    private const string PlaceholderSecret = "your-secret-key-min-32-characters-long-change-in-production-must-be-very-secure";

    [Fact]
    public void ValidateJwtConfiguration_ShouldNotThrow_WhenJwtDisabled()
    {
        var configuration = BuildConfiguration(("Jwt:Enabled", "false"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Record.Exception(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldThrow_WhenSecretIsMissing()
    {
        var configuration = BuildConfiguration();
        var environment = new FakeHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Contains("Jwt:Secret must be configured", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldThrow_WhenSecretIsTooShort()
    {
        var configuration = BuildConfiguration(("Jwt:Secret", "too-short"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Contains("at least 32 characters", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldThrow_WhenNonDevelopmentUsesPlaceholderSecret()
    {
        var configuration = BuildConfiguration(
            ("Jwt:Secret", PlaceholderSecret),
            ("Jwt:Issuer", "issuer"),
            ("Jwt:Audience", "audience"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Contains("template placeholder", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldNotThrow_WhenDevelopmentUsesPlaceholderSecretWithoutIssuerOrAudience()
    {
        var configuration = BuildConfiguration(("Jwt:Secret", PlaceholderSecret));
        var environment = new FakeHostEnvironment("Development");

        var exception = Record.Exception(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    public void ValidateJwtConfiguration_ShouldThrow_WhenNonDevelopmentMissingIssuerOrAudience(string missingKey)
    {
        var entries = new List<(string, string)> { ("Jwt:Secret", ValidSecret), ("Jwt:Issuer", "issuer"), ("Jwt:Audience", "audience") };
        entries.RemoveAll(e => e.Item1 == missingKey);
        var configuration = BuildConfiguration(entries.ToArray());
        var environment = new FakeHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Contains(missingKey, exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldNotThrow_WhenDevelopmentMissingIssuerAndAudience()
    {
        var configuration = BuildConfiguration(("Jwt:Secret", ValidSecret));
        var environment = new FakeHostEnvironment("Development");

        var exception = Record.Exception(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJwtConfiguration_ShouldNotThrow_WhenNonDevelopmentConfigurationIsValid()
    {
        var configuration = BuildConfiguration(
            ("Jwt:Secret", ValidSecret),
            ("Jwt:Issuer", "issuer"),
            ("Jwt:Audience", "audience"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Record.Exception(() =>
            JwtStartupValidation.ValidateJwtConfiguration(configuration, environment));

        Assert.Null(exception);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
