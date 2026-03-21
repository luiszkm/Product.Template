using Kernel.Infrastructure.Configurations;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AzureSpeechToTextService : ISpeechToTextService
{
    private readonly AzureCognitiveOptions _opts;

    public AzureSpeechToTextService(IOptions<AiOptions> options)
    {
        _opts = options.Value.AzureCognitive;
    }

    public async Task<SttResult> TranscribeAsync(
        byte[] audioBytes,
        SttOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var config = SpeechConfig.FromSubscription(_opts.SpeechServiceKey, _opts.SpeechServiceRegion);

        if (options?.Language is not null)
            config.SpeechRecognitionLanguage = options.Language;

        if (options?.Timestamps == true)
        {
            config.RequestWordLevelTimestamps();
            config.OutputFormat = OutputFormat.Detailed;
        }

        using var audioStream = AudioInputStream.CreatePushStream();
        audioStream.Write(audioBytes);
        audioStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(audioStream);
        using var recognizer = new SpeechRecognizer(config, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            throw new InvalidOperationException($"STT transcription failed: {cancellation.ErrorDetails}");
        }

        var segments = options?.Timestamps == true
            ? MapSegments(result)
            : null;

        return new SttResult(
            Text: result.Text,
            Language: options?.Language,
            Segments: segments);
    }

    private static IReadOnlyList<SttSegment> MapSegments(SpeechRecognitionResult result)
    {
        var detailedResults = result.Best();
        var best = detailedResults?.FirstOrDefault();
        if (best?.Words is null) return [];

        const double ticksPerSecond = 10_000_000.0;

        return best.Words
            .Select(w => new SttSegment(
                Start: w.Offset / ticksPerSecond,
                End: (w.Offset + w.Duration) / ticksPerSecond,
                Text: w.Word))
            .ToList();
    }
}
