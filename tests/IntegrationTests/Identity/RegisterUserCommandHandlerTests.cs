using IntegrationTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private RegisterUserCommandHandler CreateHandler() => new(
        _fixture.UserRepository(),
        _fixture.HashServices,
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        NullLogger<RegisterUserCommandHandler>.Instance);

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

    public void Dispose() => _fixture.Dispose();
}
