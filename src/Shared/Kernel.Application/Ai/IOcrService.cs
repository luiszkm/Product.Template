namespace Product.Template.Kernel.Application.Ai;

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default);
}
