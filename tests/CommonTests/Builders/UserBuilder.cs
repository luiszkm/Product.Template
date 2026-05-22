using Bogus;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace CommonTests.Builders;

public sealed class UserBuilder
{
    private Guid _tenantId = WellKnownTenants.Public;
    private string _email;
    private string _passwordHash;
    private string _firstName;
    private string _lastName;
    private bool _emailConfirmed;

    public UserBuilder()
    {
        var faker = new Faker();
        _email = faker.Internet.Email();
        _passwordHash = "hashed:password";
        _firstName = faker.Name.FirstName();
        _lastName = faker.Name.LastName();
    }

    public UserBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder WithConfirmedEmail() { _emailConfirmed = true; return this; }

    public User Build()
    {
        var user = User.Create(_tenantId, _email, _passwordHash, _firstName, _lastName);
        if (_emailConfirmed)
            user.ConfirmEmail();
        return user;
    }

    public List<User> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new UserBuilder()
                .WithTenantId(_tenantId)
                .Build())
            .ToList();
}
