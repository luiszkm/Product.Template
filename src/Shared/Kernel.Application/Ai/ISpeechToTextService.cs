namespace Product.Template.Kernel.Application.Ai;

public interface ISpeechToTextService
{
    Task<SttResult> TranscribeAsync(byte[] audioBytes, SttOptions? options = null, CancellationToken cancellationToken = default);
}
