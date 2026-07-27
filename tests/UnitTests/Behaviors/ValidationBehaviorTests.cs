using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Product.Template.Kernel.Application.Behaviors;

namespace UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record Ping : IRequest<string>;

    private sealed class AlwaysValid : AbstractValidator<Ping>
    {
    }

    private sealed class AlwaysInvalid : AbstractValidator<Ping>
    {
        public AlwaysInvalid()
        {
            RuleFor(p => p).Custom((_, context) =>
                context.AddFailure(new ValidationFailure("Ping", "always invalid")));
        }
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsRegistered()
    {
        var behavior = new ValidationBehavior<Ping, string>(Array.Empty<IValidator<Ping>>());
        var called = false;

        var result = await behavior.Handle(new Ping(), (_) =>
        {
            called = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenAllValidatorsPass()
    {
        var behavior = new ValidationBehavior<Ping, string>(new IValidator<Ping>[] { new AlwaysValid() });

        var result = await behavior.Handle(new Ping(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenAnyValidatorFails()
    {
        var behavior = new ValidationBehavior<Ping, string>(new IValidator<Ping>[] { new AlwaysValid(), new AlwaysInvalid() });

        var next = new Func<CancellationToken, Task<string>>(_ => throw new InvalidOperationException("next should not run"));

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new Ping(), ct => next(ct), CancellationToken.None));
    }
}
