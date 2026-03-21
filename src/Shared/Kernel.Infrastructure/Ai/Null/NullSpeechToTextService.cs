using Product.Template.Kernel.Application.Ai;
using Product.Template.Kernel.Application.Exceptions;

namespace Kernel.Infrastructure.Ai.Null;

internal sealed class NullSpeechToTextService : ISpeechToTextService
{
    private const string Message = "AI capabilities are not enabled in this environment. Set FeatureFlags:EnableAI = true to enable.";

    public Task<SttResult> TranscribeAsync(byte[] audioBytes, SttOptions? options = null, CancellationToken cancellationToken = default)
        => throw new BusinessRuleException(Message);
}
