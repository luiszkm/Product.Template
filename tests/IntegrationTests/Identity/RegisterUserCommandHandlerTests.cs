using IntegrationTests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private RegisterUserCommandHandler CreateHandler(IConfiguration? configuration = null) => new(
        _fixture.UserRepository(),
        _fixture.HashServices,
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        configuration ?? CreateConfiguration(),
        NullLogger<RegisterUserCommandHandler>.Instance);

    private static IConfiguration CreateConfiguration(bool allowPublicRegistration = true) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:AllowPublicRegistration"] = allowPublicRegistration.ToString()
            })
            .Build();

    [Fact]
    public async Task Handle_ShouldRegisterUser_WhenEmailIsUnique()
    {
        var command = new RegisterUserCommand("new@test.com", "Pass@123", "Jane", "Doe");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenEmailAlreadyExists()
    {
        await _fixture.SeedUserAsync("duplicate@test.com");

        var command = new RegisterUserCommand("duplicate@test.com", "Pass@123", "Jane", "Doe");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldPersistUser_WhenRegistrationSucceeds()
    {
        var command = new RegisterUserCommand("persist@test.com", "Pass@123", "Alice", "Smith");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        var persisted = await _fixture.UserRepository().GetByIdAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("persist@test.com", persisted.Email.Value);
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenPublicRegistrationDisabled()
    {
        var command = new RegisterUserCommand("blocked@test.com", "Pass@123", "Jane", "Doe");
        var handler = CreateHandler(CreateConfiguration(allowPublicRegistration: false));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
