using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Product.Template.Api.Configurations;

namespace UnitTests.Configurations;

public class ConnectionsConfigurationsTests
{
    [Fact]
    public void ValidateConnectionStrings_ShouldNotThrow_WhenEnvironmentIsDevelopment()
    {
        var configuration = BuildConfiguration(("ConnectionStrings:AppDb", "Password=YourStrong!Pass123;"));
        var environment = new FakeHostEnvironment("Development");

        var exception = Record.Exception(() =>
            ConnectionsConfigurations.ValidateConnectionStrings(configuration, environment));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateConnectionStrings_ShouldThrow_WhenNonDevelopmentUsesPlaceholderPassword()
    {
        var configuration = BuildConfiguration(("ConnectionStrings:AppDb", "Server=db;Password=YourStrong!Pass123;"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectionsConfigurations.ValidateConnectionStrings(configuration, environment));

        Assert.Contains("AppDb", exception.Message);
        Assert.Contains("placeholder", exception.Message);
    }

    [Fact]
    public void ValidateConnectionStrings_ShouldNotThrow_WhenNonDevelopmentUsesRealPassword()
    {
        var configuration = BuildConfiguration(("ConnectionStrings:AppDb", "Server=db;Password=Rk8$vL2pQzT9;"));
        var environment = new FakeHostEnvironment("Production");

        var exception = Record.Exception(() =>
            ConnectionsConfigurations.ValidateConnectionStrings(configuration, environment));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateConnectionStrings_ShouldNotThrow_WhenNoConnectionStringsConfigured()
    {
        var configuration = BuildConfiguration();
        var environment = new FakeHostEnvironment("Production");

        var exception = Record.Exception(() =>
            ConnectionsConfigurations.ValidateConnectionStrings(configuration, environment));

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
