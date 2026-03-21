namespace Product.Template.Kernel.Application.Ai;

public sealed record AiUsageRecord(
    string Service,
    string Provider,
    string Model,
    string Module,
    string Operation,
    long TenantId,
    int? TokensUsed,
    TimeSpan Latency,
    bool Success,
    string? ErrorCode = null
);
