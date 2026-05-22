using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Handlers.User;
using Product.Template.Core.Identity.Application.Handlers.User.Commands;
using Product.Template.Core.Identity.Infrastructure.Security;
using Product.Template.Kernel.Application.Exceptions;

namespace IntegrationTests.Identity;

public class ConfirmEmailCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();
    private readonly EmailConfirmationTokenService _tokenService;

    public ConfirmEmailCommandHandlerTests()
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "integration-test-secret-at-least-32-chars-long"
        });
        _tokenService = new EmailConfirmationTokenService(jwtSettings);
    }

    private ConfirmEmailCommandHandler CreateHandler() => new(
        _fixture.UserRepository(),
        _tokenService,
        _fixture.UnitOfWork(),
        NullLogger<ConfirmEmailCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldConfirmEmail_WhenTokenIsValid()
    {
        var user = new UserBuilder().WithEmail("confirm@test.com").Build();
        await _fixture.SeedUserAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.SecurityStamp);

        await CreateHandler().Handle(new ConfirmEmailCommand(user.Id, token), CancellationToken.None);

        var updated = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.True(updated.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenEmailAlreadyConfirmed()
    {
        var user = new UserBuilder().WithEmail("already-confirmed@test.com").WithConfirmedEmail().Build();
        await _fixture.SeedUserAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.SecurityStamp);

        await CreateHandler().Handle(new ConfirmEmailCommand(user.Id, token), CancellationToken.None);
        await CreateHandler().Handle(new ConfirmEmailCommand(user.Id, token), CancellationToken.None);

        var updated = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.True(updated.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenTokenIsInvalid()
    {
        var user = new UserBuilder().WithEmail("invalid-token@test.com").Build();
        await _fixture.SeedUserAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handle(new ConfirmEmailCommand(user.Id, "invalid-token"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var token = _tokenService.GenerateToken(userId, Guid.NewGuid().ToString("N"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new ConfirmEmailCommand(userId, token), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
