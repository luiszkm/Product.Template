using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Kernel.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AzureOpenAiLlmService : ILlmService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly AiOptions _options;

    public AzureOpenAiLlmService(AzureOpenAIClient azureClient, IOptions<AiOptions> options)
    {
        _azureClient = azureClient;
        _options = options.Value;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var deployment = request.Model ?? _options.DefaultModel;
        var chatClient = _azureClient.GetChatClient(deployment);
        var messages = BuildMessages(request);
        var chatOptions = BuildChatOptions(request);

        var sw = Stopwatch.StartNew();
        var response = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
        sw.Stop();

        var completion = response.Value;

        return new LlmResponse(
            Text: completion.Content.FirstOrDefault()?.Text ?? string.Empty,
            PromptTokens: completion.Usage?.InputTokenCount ?? 0,
            CompletionTokens: completion.Usage?.OutputTokenCount ?? 0,
            TotalTokens: completion.Usage?.TotalTokenCount ?? 0,
            Model: deployment,
            Latency: sw.Elapsed,
            ToolCalls: MapToolCalls(completion.ToolCalls)
        );
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var deployment = request.Model ?? _options.DefaultModel;
        var chatClient = _azureClient.GetChatClient(deployment);
        var messages = BuildMessages(request);
        var chatOptions = BuildChatOptions(request);

        await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                    yield return part.Text;
            }
        }
    }

    private static List<ChatMessage> BuildMessages(LlmRequest request)
    {
        var messages = new List<ChatMessage>();

        if (request.SystemPrompt is not null)
            messages.Add(ChatMessage.CreateSystemMessage(request.SystemPrompt));

        if (request.History is not null)
        {
            foreach (var msg in request.History)
            {
                ChatMessage chatMsg = msg.Role switch
                {
                    "assistant" => ChatMessage.CreateAssistantMessage(msg.Content),
                    "tool" => ChatMessage.CreateToolMessage(msg.ToolCallId ?? string.Empty, msg.Content),
                    _ => ChatMessage.CreateUserMessage(msg.Content)
                };
                messages.Add(chatMsg);
            }
        }

        messages.Add(ChatMessage.CreateUserMessage(request.UserPrompt));
        return messages;
    }

    private ChatCompletionOptions BuildChatOptions(LlmRequest request)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = request.Temperature,
            MaxOutputTokenCount = request.MaxTokens ?? _options.MaxTokens
        };

        if (request.Tools is { Count: > 0 })
        {
            foreach (var tool in request.Tools)
            {
                var toolDefinition = ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    BinaryData.FromString(tool.InputSchema.ToJsonString()));
                options.Tools.Add(toolDefinition);
            }
        }

        return options;
    }

    private static IReadOnlyList<Product.Template.Kernel.Application.Ai.ToolCall>? MapToolCalls(
        IReadOnlyList<ChatToolCall> toolCalls)
    {
        if (toolCalls is not { Count: > 0 }) return null;

        return toolCalls
            .Select(tc => new Product.Template.Kernel.Application.Ai.ToolCall(
                Id: tc.Id,
                Name: tc.FunctionName,
                Parameters: JsonNode.Parse(tc.FunctionArguments.ToString())?.AsObject() ?? new JsonObject()))
            .ToList();
    }
}
