using System.Text.Json.Nodes;
using Product.Template.Core.Ai.Application.Agent;
using Product.Template.Kernel.Application.Ai;

namespace UnitTests.Ai;

public class ToolRegistryTests
{
    [Fact]
    public void GetDefinitions_ShouldReturnAllRegisteredTools()
    {
        var tools = new ITool[] { new EchoTool("tool_a"), new EchoTool("tool_b") };
        var registry = new ToolRegistry(tools);

        var definitions = registry.GetDefinitions();

        Assert.Equal(2, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "tool_a");
        Assert.Contains(definitions, d => d.Name == "tool_b");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallMatchingTool_WhenToolExists()
    {
        var tool = new EchoTool("greet");
        var registry = new ToolRegistry([tool]);

        var result = await registry.ExecuteAsync(new ToolCall("id-1", "greet", new JsonObject()), default);

        Assert.Equal("{\"echo\":\"greet\"}", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorJson_WhenToolNotFound()
    {
        var registry = new ToolRegistry([]);

        var result = await registry.ExecuteAsync(new ToolCall("id-1", "nonexistent", new JsonObject()), default);

        Assert.Contains("error", result);
        Assert.Contains("nonexistent", result);
    }

    private sealed class EchoTool : ITool
    {
        public EchoTool(string name) =>
            Definition = new ToolDefinition(name, $"Echo tool {name}", new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject()
            });

        public ToolDefinition Definition { get; }

        public Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default) =>
            Task.FromResult($"{{\"echo\":\"{Definition.Name}\"}}");
    }
}
