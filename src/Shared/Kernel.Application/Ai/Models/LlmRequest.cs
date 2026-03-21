namespace Product.Template.Kernel.Application.Ai;

public sealed record LlmRequest(
    string UserPrompt,
    string? SystemPrompt = null,
    string? Model = null,
    float Temperature = 0.2f,
    int? MaxTokens = null,
    IReadOnlyList<LlmMessage>? History = null,
    IReadOnlyList<ToolDefinition>? Tools = null
);

public sealed record LlmMessage(
    string Role,
    string Content,
    string? ToolCallId = null
);
