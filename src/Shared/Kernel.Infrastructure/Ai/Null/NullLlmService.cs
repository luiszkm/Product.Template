using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Exceptions;

namespace Kernel.Infrastructure.Ai.Null;

internal sealed class NullLlmService : ILlmService
{
    private const string Message = "AI capabilities are not enabled in this environment. Set FeatureFlags:EnableAI = true to enable.";

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);

    public IAsyncEnumerable<string> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);
}
