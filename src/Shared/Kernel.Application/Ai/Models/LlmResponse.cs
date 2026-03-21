namespace Product.Template.Kernel.Application.Ai;

public sealed record LlmResponse(
    string Text,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    string Model,
    TimeSpan Latency,
    IReadOnlyList<ToolCall>? ToolCalls = null
);
