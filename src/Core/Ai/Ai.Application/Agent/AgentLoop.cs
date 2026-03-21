using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Application.Ai;

namespace Product.Template.Core.Ai.Application.Agent;

public sealed class AgentLoop
{
    private const int MaxIterations = 5;

    private readonly ILlmService _llm;
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<AgentLoop> _logger;

    public AgentLoop(ILlmService llm, ToolRegistry toolRegistry, ILogger<AgentLoop> logger)
    {
        _llm = llm;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<AgentResult> RunAsync(
        string userMessage,
        string systemPrompt,
        IReadOnlyList<LlmMessage>? history,
        CancellationToken cancellationToken)
    {
        var conversationHistory = history?.ToList() ?? [];
        var toolDefinitions = _toolRegistry.GetDefinitions();
        var iterations = 0;

        while (iterations < MaxIterations)
        {
            iterations++;

            var request = new LlmRequest(
                UserPrompt: userMessage,
                SystemPrompt: systemPrompt,
                History: conversationHistory.Count > 0 ? conversationHistory : null,
                Tools: toolDefinitions.Count > 0 ? toolDefinitions : null
            );

            var response = await _llm.CompleteAsync(request, cancellationToken);

            if (response.ToolCalls is not { Count: > 0 })
                return new AgentResult(response.Text, iterations, response.TotalTokens);

            conversationHistory.Add(new LlmMessage("assistant", response.Text ?? string.Empty));

            var toolResults = await Task.WhenAll(
                response.ToolCalls.Select(tc => ExecuteToolAsync(tc, cancellationToken)));

            foreach (var result in toolResults)
                conversationHistory.Add(new LlmMessage("tool", result.Output, result.ToolCallId));

            userMessage = string.Empty;
        }

        _logger.LogWarning("AgentLoop reached max iterations ({Max}) without a final answer", MaxIterations);

        var fallback = new LlmRequest(
            UserPrompt: "Resuma o que foi encontrado até agora com base nos dados das ferramentas.",
            SystemPrompt: systemPrompt,
            History: conversationHistory.Count > 0 ? conversationHistory : null
        );

        var fallbackResponse = await _llm.CompleteAsync(fallback, cancellationToken);
        return new AgentResult(fallbackResponse.Text, iterations, fallbackResponse.TotalTokens);
    }

    private async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken ct)
    {
        _logger.LogInformation("Executing tool {ToolName} with params {Params}",
            toolCall.Name, toolCall.Parameters.ToJsonString());

        var output = await _toolRegistry.ExecuteAsync(toolCall, ct);
        return new ToolResult(toolCall.Id, output);
    }

    private sealed record ToolResult(string ToolCallId, string Output);
}

public sealed record AgentResult(string Reply, int IterationsUsed, int TotalTokens);
