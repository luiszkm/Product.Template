using System.Text.Json.Nodes;

namespace Product.Template.Kernel.Application.Ai;

public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonObject InputSchema
);
