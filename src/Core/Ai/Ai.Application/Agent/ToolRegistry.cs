using Product.Template.Kernel.Application.Ai;

namespace Product.Template.Core.Ai.Application.Agent;

public sealed class ToolRegistry
{
    private readonly IReadOnlyDictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools) =>
        _tools = tools.ToDictionary(t => t.Definition.Name);

    public IReadOnlyList<ToolDefinition> GetDefinitions() =>
        _tools.Values.Select(t => t.Definition).ToList();

    public Task<string> ExecuteAsync(ToolCall toolCall, CancellationToken ct) =>
        _tools.TryGetValue(toolCall.Name, out var tool)
            ? tool.ExecuteAsync(toolCall, ct)
            : Task.FromResult($"{{\"error\": \"Tool '{toolCall.Name}' not found.\"}}");
}
