using System.Security.Claims;
using Kernel.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Controllers.v1;
using Product.Template.Core.Identity.Application.Queries.User;
using Product.Template.Core.Identity.Application.Queries.Users;
using Product.Template.Kernel.Application.Security;

namespace UnitTests.Security;

public class IdentityControllerAuthorizationTests
{
    [Fact]
    public async Task GetById_ShouldReturnForbid_WhenUserIsNotOwnerAndNotAdmin()
    {
        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), "User");
        var targetUserId = Guid.NewGuid();

        var controller = CreateController(currentUser, new StubMediator(_ =>
            new UserOutput(targetUserId, "x@test.com", "X", "Y", true, DateTime.UtcNow, null, ["User"])));

        var result = await controller.GetById(targetUserId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserIsOwner()
    {
        var userId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserService(userId, "User");

        var controller = CreateController(currentUser, new StubMediator(_ =>
            new UserOutput(userId, "owner@test.com", "Owner", "User", true, DateTime.UtcNow, null, ["User"])));

        var result = await controller.GetById(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<UserOutput>(ok.Value);
        Assert.Equal(userId, payload.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserIsAdmin()
    {
        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), "Admin");
        var targetUserId = Guid.NewGuid();

        var controller = CreateController(currentUser, new StubMediator(_ =>
            new UserOutput(targetUserId, "adminview@test.com", "Admin", "View", true, DateTime.UtcNow, null, ["User"])));

        var result = await controller.GetById(targetUserId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<UserOutput>(ok.Value);
        Assert.Equal(targetUserId, payload.Id);
    }

    private static IdentityController CreateController(ICurrentUserService currentUserService, IMediator mediator)
    {
        return new IdentityController(
            mediator,
            NullLogger<IdentityController>.Instance,
            new FakeAuthenticationProviderFactory(),
            currentUserService);
    }

    private sealed class StubMediator(Func<object, object> responder) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)responder(request));

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(responder(request));

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => EmptyAsync<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => EmptyAsync<object?>();
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private sealed class FakeCurrentUserService(Guid? userId, params string[] roles) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? Email => "user@test.com";
        public string? UserName => "tester";
        public bool IsAuthenticated => userId.HasValue;
        public IEnumerable<Claim> Claims => roles.Select(r => new Claim(ClaimTypes.Role, r));
    }

    private sealed class FakeAuthenticationProviderFactory : IAuthenticationProviderFactory
    {
        public IAuthenticationProvider GetProvider(string providerName) => throw new InvalidOperationException("Not needed");
        public IEnumerable<string> GetAvailableProviders() => ["jwt"];
        public bool IsProviderAvailable(string providerName) => providerName == "jwt";
    }
}
