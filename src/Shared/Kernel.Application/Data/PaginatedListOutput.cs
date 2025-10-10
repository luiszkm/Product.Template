

namespace Kernel.Application.Data;
public class PaginatedListOutput<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int Page { get; }
    public int PerPage { get; }
    public int TotalCount { get; }
    public bool HasNextPage => Page * PerPage < TotalCount;

    public PaginatedListOutput(IReadOnlyCollection<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        Page = pageNumber;
        PerPage = pageSize;
        TotalCount = totalCount;
    }
}