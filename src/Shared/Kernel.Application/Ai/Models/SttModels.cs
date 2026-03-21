namespace Product.Template.Kernel.Application.Ai;

public sealed record SttOptions(string? Language = null, bool Timestamps = false);
public sealed record SttResult(string Text, string? Language, IReadOnlyList<SttSegment>? Segments);
public sealed record SttSegment(double Start, double End, string Text);
