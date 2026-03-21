using Kernel.Infrastructure.Configurations;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AzureTextToSpeechService : ITextToSpeechService
{
    private readonly AzureCognitiveOptions _opts;

    public AzureTextToSpeechService(IOptions<AiOptions> options)
    {
        _opts = options.Value.AzureCognitive;
    }

    public async Task<byte[]> SynthesizeAsync(
        string text,
        TtsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var config = SpeechConfig.FromSubscription(_opts.SpeechServiceKey, _opts.SpeechServiceRegion);

        if (options?.Voice is not null)
            config.SpeechSynthesisVoiceName = options.Voice;

        if (options?.Language is not null)
            config.SpeechSynthesisLanguage = options.Language;

        config.SetSpeechSynthesisOutputFormat(MapOutputFormat(options?.OutputFormat ?? "mp3"));

        using var stream = AudioOutputStream.CreatePullStream();
        using var audioConfig = AudioConfig.FromStreamOutput(stream);
        using var synthesizer = new SpeechSynthesizer(config, audioConfig);

        var ssml = options?.Speed is not null
            ? BuildSsml(text, options)
            : text;

        var result = options?.Speed is not null
            ? await synthesizer.SpeakSsmlAsync(ssml)
            : await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            throw new InvalidOperationException($"TTS synthesis failed: {cancellation.ErrorDetails}");
        }

        return result.AudioData;
    }

    private static SpeechSynthesisOutputFormat MapOutputFormat(string format) =>
        format.ToLowerInvariant() switch
        {
            "wav" => SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm,
            "ogg" => SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus,
            _ => SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3
        };

    private static string BuildSsml(string text, TtsOptions options)
    {
        var rate = options.Speed is not null ? $"{options.Speed:F1}" : "1.0";
        var voice = options.Voice ?? "en-US-JennyNeural";
        return $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">
                <voice name="{voice}">
                    <prosody rate="{rate}">{System.Security.SecurityElement.Escape(text)}</prosody>
                </voice>
            </speak>
            """;
    }
}
