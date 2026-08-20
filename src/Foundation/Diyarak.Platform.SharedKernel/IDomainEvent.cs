namespace Diyarak.Platform.SharedKernel;

public interface IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOnUtc { get; }
}
