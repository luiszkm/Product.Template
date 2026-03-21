using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Ai.Application.Agent;
using Product.Template.Kernel.Application.Ai;

namespace UnitTests.Ai;

public class AgentLoopTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnDirectReply_WhenNoToolCallsRequested()
    {
        var llm = new StubLlmService("Hello from the model", toolCalls: null);
        var registry = new ToolRegistry([]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("Hi", "System", null, default);

        Assert.Equal("Hello from the model", result.Reply);
        Assert.Equal(1, result.IterationsUsed);
    }

    [Fact]
    public async Task RunAsync_ShouldCallToolAndReturnFinalAnswer_WhenToolCallRequested()
    {
        var toolCalls = new List<ToolCall>
        {
            new("call-1", "get_info", new JsonObject())
        };

        var llm = new SequentialLlmService(
            first: new LlmResponse("Thinking...", 10, 5, 15, "gpt-4o-mini", TimeSpan.Zero, toolCalls),
            second: new LlmResponse("Based on the data: 42 users.", 20, 10, 30, "gpt-4o-mini", TimeSpan.Zero)
        );

        var tool = new CountingTool("get_info");
        var registry = new ToolRegistry([tool]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("How many users?", "System", null, default);

        Assert.Equal("Based on the data: 42 users.", result.Reply);
        Assert.Equal(2, result.IterationsUsed);
        Assert.Equal(1, tool.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldStopAtMaxIterations_WhenToolCallsNeverEnd()
    {
        var toolCalls = new List<ToolCall> { new("c1", "loop_tool", new JsonObject()) };
        var llm = new InfiniteToolCallLlm(toolCalls);
        var tool = new CountingTool("loop_tool");
        var registry = new ToolRegistry([tool]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("Infinite?", "System", null, default);

        Assert.Equal(5, result.IterationsUsed);
    }

    // ── Stubs ────────────────────────────────────────────────────────────────

    private sealed class StubLlmService : ILlmService
    {
        private readonly string _text;
        private readonly IReadOnlyList<ToolCall>? _toolCalls;

        public StubLlmService(string text, IReadOnlyList<ToolCall>? toolCalls)
        {
            _text = text;
            _toolCalls = toolCalls;
        }

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmResponse(_text, 10, 5, 15, "stub", TimeSpan.Zero, _toolCalls));

        public async IAsyncEnumerable<string> StreamAsync(LlmRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return _text;
            await Task.CompletedTask;
        }
    }

    private sealed class SequentialLlmService : ILlmService
    {
        private readonly LlmResponse _first;
        private readonly LlmResponse _second;
        private int _calls;

        public SequentialLlmService(LlmResponse first, LlmResponse second)
        {
            _first = first;
            _second = second;
        }

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
        {
            _calls++;
            return Task.FromResult(_calls == 1 ? _first : _second);
        }

        public async IAsyncEnumerable<string> StreamAsync(LlmRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return string.Empty;
            await Task.CompletedTask;
        }
    }

    private sealed class InfiniteToolCallLlm : ILlmService
    {
        private readonly IReadOnlyList<ToolCall> _toolCalls;

        public InfiniteToolCallLlm(IReadOnlyList<ToolCall> toolCalls) => _toolCalls = toolCalls;

        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LlmResponse(string.Empty, 5, 5, 10, "stub", TimeSpan.Zero, _toolCalls));

        public async IAsyncEnumerable<string> StreamAsync(LlmRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return string.Empty;
            await Task.CompletedTask;
        }
    }

    private sealed class CountingTool : ITool
    {
        public int CallCount { get; private set; }

        public CountingTool(string name) =>
            Definition = new ToolDefinition(name, $"Tool {name}", new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject()
            });

        public ToolDefinition Definition { get; }

        public Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult("{\"count\": 42}");
        }
    }
}
