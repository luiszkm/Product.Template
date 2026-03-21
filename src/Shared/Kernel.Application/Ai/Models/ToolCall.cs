using System.Text.Json.Nodes;

namespace Product.Template.Kernel.Application.Ai;

public sealed record ToolCall(
    string Id,
    string Name,
    JsonObject Parameters
);
