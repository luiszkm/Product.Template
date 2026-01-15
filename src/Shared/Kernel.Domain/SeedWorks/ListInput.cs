namespace Kernel.Domain.SeedWorks;

public record ListInput(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    string? SortDirection = null
    );
