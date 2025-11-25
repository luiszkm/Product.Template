using Neuraptor.ERP.Core.Identity.Domain.Events;
using Neuraptor.ERP.Core.Identity.Domain.ValueObjects;
using Neuraptor.ERP.Kernel.Domain.SeedWorks;

namespace Neuraptor.ERP.Core.Identity.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User(Guid id) : base(id) { }

    public static User Create(
        Email email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        var user = new User(Guid.NewGuid())
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));

        return user;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInEvent(Id, Email.Value));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void AddRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return;

        _userRoles.Add(UserRole.Create(Id, role.Id));
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
            _userRoles.Remove(userRole);
    }

    public bool HasRole(string roleName)
    {
        return _userRoles.Any(ur => ur.Role?.Name == roleName);
    }
}
