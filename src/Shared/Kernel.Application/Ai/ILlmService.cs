namespace Product.Template.Kernel.Application.Ai;

public interface ILlmService
{
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
