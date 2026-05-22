using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Ai.Application.Agent;
using Product.Template.Core.Ai.Application.Handlers;
using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Ai;

public class ChatCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOutput_WhenStubClientReturnsMessage()
    {
        var tenantId = WellKnownTenants.Public;
        var llm = new StubLlmService("Assistant reply");
        var agentLoop = new AgentLoop(llm, new ToolRegistry([]), NullLogger<AgentLoop>.Instance);
        var tracker = new CapturingAiUsageTracker();
        var tenantContext = new StubTenantContext(tenantId);
        var handler = new ChatCommandHandler(
            agentLoop,
            tracker,
            tenantContext,
            NullLogger<ChatCommandHandler>.Instance);

        var result = await handler.Handle(new ChatCommand("Hello"), CancellationToken.None);

        Assert.Equal("Assistant reply", result.Reply);
        Assert.Equal(1, result.IterationsUsed);
        Assert.NotNull(tracker.LastRecord);
        Assert.Equal(tenantId, tracker.LastRecord.TenantId);
        Assert.True(tracker.LastRecord.Success);
    }

    [Fact]
    public async Task Handle_ShouldTrackGuidEmpty_WhenTenantMissing()
    {
        var llm = new StubLlmService("Reply without tenant");
        var agentLoop = new AgentLoop(llm, new ToolRegistry([]), NullLogger<AgentLoop>.Instance);
        var tracker = new CapturingAiUsageTracker();
        var tenantContext = new StubTenantContext(null);
        var handler = new ChatCommandHandler(
            agentLoop,
            tracker,
            tenantContext,
            NullLogger<ChatCommandHandler>.Instance);

        var result = await handler.Handle(new ChatCommand("Hello"), CancellationToken.None);

        Assert.Equal("Reply without tenant", result.Reply);
        Assert.NotNull(tracker.LastRecord);
        Assert.Equal(Guid.Empty, tracker.LastRecord.TenantId);
    }

    private sealed class StubLlmService : ILlmService
    {
        private readonly string _text;

        public StubLlmService(string text) => _text = text;

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmResponse(_text, 10, 5, 15, "stub", TimeSpan.Zero));

        public async IAsyncEnumerable<string> StreamAsync(LlmRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return _text;
            await Task.CompletedTask;
        }
    }

    private sealed class StubTenantContext : ITenantContext
    {
        private readonly Guid? _tenantId;

        public StubTenantContext(Guid? tenantId) => _tenantId = tenantId;

        public Guid? TenantId => _tenantId;

        public string? TenantKey => _tenantId.HasValue ? "test" : null;

        public TenantConfig? Tenant => _tenantId.HasValue
            ? new TenantConfig { TenantId = _tenantId.Value, TenantKey = "test" }
            : null;

        public bool IsResolved => _tenantId.HasValue;

        public void SetTenant(TenantConfig tenant) => throw new NotSupportedException();
    }

    private sealed class CapturingAiUsageTracker : IAiUsageTracker
    {
        public AiUsageRecord? LastRecord { get; private set; }

        public Task TrackAsync(AiUsageRecord record, CancellationToken cancellationToken = default)
        {
            LastRecord = record;
            return Task.CompletedTask;
        }
    }
}
