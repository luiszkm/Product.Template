using Kernel.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Kernel.Ai;

public class AiUsageTrackerTests
{
    [Fact]
    public async Task TrackAsync_ShouldCompleteWithoutThrowing_OnSuccessfulRecord()
    {
        var sut = new AiUsageTracker(NullLogger<AiUsageTracker>.Instance);

        var record = new AiUsageRecord(
            Service: "llm",
            Provider: "azure-openai",
            Model: "gpt-4o-mini",
            Module: "identity",
            Operation: "SummarizeUserCommandHandler",
            TenantId: WellKnownTenants.Public,
            TokensUsed: 150,
            Latency: TimeSpan.FromMilliseconds(320),
            Success: true);

        var ex = await Record.ExceptionAsync(() => sut.TrackAsync(record));

        Assert.Null(ex);
    }

    [Fact]
    public async Task TrackAsync_ShouldCompleteWithoutThrowing_OnFailureRecord()
    {
        var sut = new AiUsageTracker(NullLogger<AiUsageTracker>.Instance);

        var record = new AiUsageRecord(
            Service: "ocr",
            Provider: "azure-cognitive",
            Model: "prebuilt-read",
            Module: "documents",
            Operation: "ExtractDocumentCommandHandler",
            TenantId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TokensUsed: null,
            Latency: TimeSpan.FromMilliseconds(5000),
            Success: false,
            ErrorCode: "ServiceUnavailable");

        var ex = await Record.ExceptionAsync(() => sut.TrackAsync(record));

        Assert.Null(ex);
    }

    [Fact]
    public async Task TrackAsync_ShouldAcceptNullTokensUsed_WhenServiceDoesNotTrackTokens()
    {
        var sut = new AiUsageTracker(NullLogger<AiUsageTracker>.Instance);

        var record = new AiUsageRecord(
            Service: "tts",
            Provider: "azure-cognitive",
            Model: "neural",
            Module: "notifications",
            Operation: "SynthesizeAudioCommandHandler",
            TenantId: WellKnownTenants.Public,
            TokensUsed: null,
            Latency: TimeSpan.FromMilliseconds(800),
            Success: true);

        var ex = await Record.ExceptionAsync(() => sut.TrackAsync(record));

        Assert.Null(ex);
    }
}
