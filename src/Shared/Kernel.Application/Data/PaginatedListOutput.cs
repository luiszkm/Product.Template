namespace Product.Template.Kernel.Application.Data;

public record PaginatedListOutput<TItem>(
    int Page,
    int PerPage,
    int TotalCount,
    IReadOnlyList<TItem> Items);

