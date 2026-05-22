namespace Product.Template.Api.HealthChecks;

public static class HealthChecksUiSupport
{
    public const string TrackingIssueUrl = "https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues";

    public static HealthChecksUiStatus Evaluate()
    {
        return new HealthChecksUiStatus(
            IsSupported: false,
            LatestKnownPackageVersion: "9.0.0",
            BlockingReason: "AspNetCore.HealthChecks.UI 9.0.0 pulls Duende.IdentityModel 5.2.0, which conflicts with Microsoft.IdentityModel 8.x required by .NET 10.",
            Recommendation: "Use /health, /health/ready, and /health/live JSON endpoints. Re-evaluate when NuGet lists a release built for .NET 10 without the IdentityModel conflict.");
    }
}

public sealed record HealthChecksUiStatus(
    bool IsSupported,
    string LatestKnownPackageVersion,
    string BlockingReason,
    string Recommendation);
