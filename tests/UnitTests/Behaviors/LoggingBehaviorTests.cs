using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Application.Behaviors;

namespace UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record Ping : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldReturnNextResponse_WhenRequestSucceeds()
    {
        var behavior = new LoggingBehavior<Ping, string>(NullLogger<LoggingBehavior<Ping, string>>.Instance);

        var result = await behavior.Handle(new Ping(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ShouldRethrow_WhenNextThrows()
    {
        var behavior = new LoggingBehavior<Ping, string>(NullLogger<LoggingBehavior<Ping, string>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(new Ping(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));
    }
}
