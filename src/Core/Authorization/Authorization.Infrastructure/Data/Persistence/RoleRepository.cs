using Kernel.Domain.SeedWorks;
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Authorization.Domain.Entities;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Authorization.Infrastructure.Data.Persistence;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.Set<Role>()
            .FirstOrDefaultAsync(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken);

    public async Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Role>()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await _context.Set<Role>().AddAsync(role, cancellationToken);

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Set<Role>().Update(role);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Set<Role>().Remove(role);
        return Task.CompletedTask;
    }

    public async Task<PaginatedListOutput<Role>> ListAllAsync(ListInput listInput, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Role>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(listInput.SearchTerm))
        {
            var term = listInput.SearchTerm.Trim();
            query = query.Where(r =>
                r.Name.Contains(term) ||
                r.Description.Contains(term));
        }

        query = ApplySort(query, listInput.SortBy, listInput.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .Skip((listInput.PageNumber - 1) * listInput.PageSize)
            .Take(listInput.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedListOutput<Role>(
            PageNumber: listInput.PageNumber,
            PageSize: listInput.PageSize,
            TotalCount: totalCount,
            Data: roles);
    }

    private static IQueryable<Role> ApplySort(IQueryable<Role> query, string? sortBy, string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id);

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.Trim().ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(r => r.Name).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Name).ThenBy(r => r.Id),
            "description" => descending
                ? query.OrderByDescending(r => r.Description).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Description).ThenBy(r => r.Id),
            "createdat" => descending
                ? query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
                : query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            _ => query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
        };
    }
}
