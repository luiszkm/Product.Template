using Kernel.Domain.SeedWorks;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Persistence;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _context;

    public PermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Permission>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.Set<Permission>()
            .FirstOrDefaultAsync(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken);

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
        => await _context.Set<Permission>().AddAsync(permission, cancellationToken);

    public Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Set<Permission>().Update(permission);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Set<Permission>().Remove(permission);
        return Task.CompletedTask;
    }

    public async Task<PaginatedListOutput<Permission>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Permission>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(listInput.SearchTerm))
        {
            var term = listInput.SearchTerm.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Description.Contains(term));
        }

        query = ApplySort(query, listInput.SortBy, listInput.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);

        var permissions = await query
            .Skip((listInput.PageNumber - 1) * listInput.PageSize)
            .Take(listInput.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedListOutput<Permission>(
            PageNumber: listInput.PageNumber,
            PageSize: listInput.PageSize,
            TotalCount: totalCount,
            Data: permissions);
    }

    private static IQueryable<Permission> ApplySort(IQueryable<Permission> query, string? sortBy, string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.Trim().ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "description" => descending
                ? query.OrderByDescending(p => p.Description).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Description).ThenBy(p => p.Id),
            "createdat" => descending
                ? query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                : query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };
    }
}
