namespace Diyarak.Platform.SharedKernel;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(DateTimeOffset? occurredOnUtc = null)
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = occurredOnUtc ?? DateTimeOffset.UtcNow;
    }
    public Guid EventId { get; }
    public DateTimeOffset OccurredOnUtc { get; }
}
