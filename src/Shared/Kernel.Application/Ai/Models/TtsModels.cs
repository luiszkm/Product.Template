namespace Product.Template.Kernel.Application.Ai;

public sealed record TtsOptions(
    string? Voice = null,
    string? Language = null,
    float? Speed = null,
    string OutputFormat = "mp3"
);
