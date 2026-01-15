namespace Product.Template.Kernel.Domain.SeedWorks;

public record PaginatedListOutput<TItem>(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<TItem> Data);

