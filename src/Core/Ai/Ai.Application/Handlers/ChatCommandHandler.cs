using Microsoft.Extensions.Logging;
using Product.Template.Core.Ai.Application.Agent;
using Product.Template.Core.Ai.Application.Prompts;
using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Messaging.Interfaces;

using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Ai.Application.Handlers;

internal sealed class ChatCommandHandler : ICommandHandler<ChatCommand, ChatOutput>
{
    private readonly AgentLoop _agentLoop;
    private readonly IAiUsageTracker _tracker;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ChatCommandHandler> _logger;

    public ChatCommandHandler(
        AgentLoop agentLoop,
        IAiUsageTracker tracker,
        ITenantContext tenantContext,
        ILogger<ChatCommandHandler> logger)
    {
        _agentLoop = agentLoop;
        _tracker = tracker;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<ChatOutput> Handle(ChatCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI chat request for tenant {TenantId}", _tenantContext.TenantId);

        var started = DateTime.UtcNow;
        AgentResult? result = null;
        string? errorCode = null;

        try
        {
            result = await _agentLoop.RunAsync(
                command.Message,
                AgentSystemPrompt.Text,
                command.History,
                cancellationToken);

            return new ChatOutput(result.Reply, result.IterationsUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentLoop failed for tenant {TenantId}", _tenantContext.TenantId);
            errorCode = ex.GetType().Name;
            throw;
        }
        finally
        {
            var latency = DateTime.UtcNow - started;
            await _tracker.TrackAsync(new AiUsageRecord(
                Service: "llm",
                Provider: "azure-openai",
                Model: "agent",
                Module: "ai",
                Operation: "chat",
                TenantId: _tenantContext.TenantId ?? 0,
                TokensUsed: result?.TotalTokens,
                Latency: latency,
                Success: errorCode is null,
                ErrorCode: errorCode
            ), cancellationToken);
        }
    }
}
