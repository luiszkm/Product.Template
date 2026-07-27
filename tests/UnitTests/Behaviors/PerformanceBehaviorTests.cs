using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Kernel.Application.Behaviors;

namespace UnitTests.Behaviors;

public class PerformanceBehaviorTests
{
    private sealed record Ping : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldReturnNextResponse_WhenRequestIsFast()
    {
        var behavior = new PerformanceBehavior<Ping, string>(
            NullLogger<PerformanceBehavior<Ping, string>>.Instance,
            slowRequestThresholdMs: 500);

        var result = await behavior.Handle(new Ping(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ShouldStillReturnResponse_WhenRequestExceedsThreshold()
    {
        var behavior = new PerformanceBehavior<Ping, string>(
            NullLogger<PerformanceBehavior<Ping, string>>.Instance,
            slowRequestThresholdMs: 0);

        var result = await behavior.Handle(new Ping(), async ct =>
        {
            await Task.Delay(5, ct);
            return "ok";
        }, CancellationToken.None);

        Assert.Equal("ok", result);
    }
}
