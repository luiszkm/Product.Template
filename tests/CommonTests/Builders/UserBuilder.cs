using Bogus;
using Product.Template.Core.Identity.Domain.Entities;

namespace CommonTests.Builders;

public sealed class UserBuilder
{
    private readonly Faker _faker = new();
    private long _tenantId = 1L;
    private string _email;
    private string _passwordHash = "hashed:Pass@123";
    private string _firstName;
    private string _lastName;
    private bool _confirmEmail;

    public UserBuilder()
    {
        _email = _faker.Internet.Email();
        _firstName = _faker.Name.FirstName();
        _lastName = _faker.Name.LastName();
    }

    public UserBuilder WithTenantId(long tenantId) { _tenantId = tenantId; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder WithConfirmedEmail() { _confirmEmail = true; return this; }

    public User Build()
    {
        var user = User.Create(_tenantId, _email, _passwordHash, _firstName, _lastName);
        if (_confirmEmail)
            user.ConfirmEmail();
        return user;
    }

    public List<User> BuildMany(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new UserBuilder()
                .WithTenantId(_tenantId)
                .WithConfirmedEmail()
                .Build())
            .ToList();
}
