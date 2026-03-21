namespace Product.Template.Kernel.Application.Ai;

public sealed record OcrRequest(
    byte[] FileBytes,
    string MimeType,
    string? Language = null,
    bool ExtractTables = false,
    bool ExtractKeyValues = false
);

public sealed record OcrResult(
    string FullText,
    IReadOnlyList<OcrPage> Pages,
    IReadOnlyList<OcrTable> Tables,
    IReadOnlyDictionary<string, string> KeyValues,
    float Confidence
);

public sealed record OcrPage(int Number, string Text, float Confidence);
public sealed record OcrTable(int PageNumber, IReadOnlyList<IReadOnlyList<string>> Rows);
