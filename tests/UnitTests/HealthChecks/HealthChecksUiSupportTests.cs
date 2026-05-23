using Product.Template.Api.HealthChecks;

namespace UnitTests.HealthChecks;

public class HealthChecksUiSupportTests
{
    [Fact]
    public void Evaluate_ShouldReportUnsupported_OnNet10()
    {
        var status = HealthChecksUiSupport.Evaluate();

        Assert.False(status.IsSupported);
        Assert.Equal("9.0.0", status.LatestKnownPackageVersion);
        Assert.Contains("IdentityModel", status.BlockingReason);
        Assert.True(status.LastCheckedUtc <= DateTime.UtcNow);
        Assert.True(status.LastCheckedUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}
