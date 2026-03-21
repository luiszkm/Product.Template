namespace Product.Template.Kernel.Application.Ai;

public sealed record EmbeddingResult(
    float[] Vector,
    int Dimensions,
    int TokensUsed,
    string Model
);
