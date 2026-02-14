using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Core.Identity.Domain.Entities;

public class Role : Entity<Guid>, IMultiTenantEntity
{
    public long TenantId { get; set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role(Guid id) : base(id) { }

    public static Role Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        return new Role(Guid.NewGuid())
        {
            Name = name,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }
}
