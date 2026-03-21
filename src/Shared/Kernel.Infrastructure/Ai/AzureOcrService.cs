using Azure;
using Azure.AI.DocumentIntelligence;
using Kernel.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Application.Ai;

namespace Kernel.Infrastructure.Ai;

internal sealed class AzureOcrService : IOcrService
{
    private readonly DocumentIntelligenceClient _client;

    public AzureOcrService(IOptions<AiOptions> options)
    {
        var opts = options.Value.AzureCognitive;
        _client = new DocumentIntelligenceClient(
            new Uri(opts.DocumentIntelligenceEndpoint),
            new AzureKeyCredential(opts.DocumentIntelligenceApiKey));
    }

    public async Task<OcrResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        var analyzeOptions = new AnalyzeDocumentOptions("prebuilt-read", BinaryData.FromBytes(request.FileBytes));

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            analyzeOptions,
            cancellationToken: cancellationToken);

        var result = operation.Value;

        var pages = result.Pages
            .Select(p => new OcrPage(
                Number: p.PageNumber,
                Text: string.Join(" ", p.Words.Select(w => w.Content)),
                Confidence: p.Words.Any() ? (float)p.Words.Average(w => (double)w.Confidence) : 0f))
            .ToList();

        var tables = new List<OcrTable>();
        if (request.ExtractTables && result.Tables is not null)
        {
            tables = result.Tables
                .Select(t =>
                {
                    var maxRow = t.Cells.Max(c => c.RowIndex) + 1;
                    var maxCol = t.Cells.Max(c => c.ColumnIndex) + 1;

                    var rows = Enumerable.Range(0, maxRow)
                        .Select(r => (IReadOnlyList<string>)Enumerable.Range(0, maxCol)
                            .Select(c => t.Cells
                                .FirstOrDefault(cell => cell.RowIndex == r && cell.ColumnIndex == c)
                                ?.Content ?? string.Empty)
                            .ToList())
                        .ToList();

                    var pageNumber = t.BoundingRegions is { Count: > 0 }
                        ? t.BoundingRegions[0].PageNumber
                        : 0;

                    return new OcrTable(
                        PageNumber: pageNumber,
                        Rows: rows);
                })
                .ToList();
        }

        var keyValues = new Dictionary<string, string>();
        if (request.ExtractKeyValues && result.KeyValuePairs is not null)
        {
            foreach (var kvp in result.KeyValuePairs)
            {
                if (kvp.Key?.Content is not null && kvp.Value?.Content is not null)
                    keyValues[kvp.Key.Content] = kvp.Value.Content;
            }
        }

        var fullText = string.Join("\n\n", pages.Select(p => p.Text));
        var avgConfidence = pages.Count > 0 ? pages.Average(p => p.Confidence) : 0f;

        return new OcrResult(
            FullText: fullText,
            Pages: pages,
            Tables: tables,
            KeyValues: keyValues,
            Confidence: avgConfidence);
    }
}
