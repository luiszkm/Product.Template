
namespace Kernel.Domain.SeedWorks;

public interface IDomainEvent
{
    public DateTime OccurredOn { get; }
}
