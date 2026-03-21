namespace Product.Template.Kernel.Application.Ai;

public interface ITextToSpeechService
{
    Task<byte[]> SynthesizeAsync(string text, TtsOptions? options = null, CancellationToken cancellationToken = default);
}
