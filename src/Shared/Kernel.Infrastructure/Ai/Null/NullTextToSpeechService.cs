using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Exceptions;

namespace Kernel.Infrastructure.Ai.Null;

internal sealed class NullTextToSpeechService : ITextToSpeechService
{
    private const string Message = "AI capabilities are not enabled in this environment. Set FeatureFlags:EnableAI = true to enable.";

    public Task<byte[]> SynthesizeAsync(string text, TtsOptions? options = null, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);
}
