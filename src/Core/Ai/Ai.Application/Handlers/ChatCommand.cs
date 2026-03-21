using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.Ai.Application.Handlers;

public record ChatCommand(
    string Message,
    IReadOnlyList<LlmMessage>? History = null
) : ICommand<ChatOutput>;
