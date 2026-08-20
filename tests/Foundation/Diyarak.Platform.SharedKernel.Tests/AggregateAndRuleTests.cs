namespace Diyarak.Platform.SharedKernel.Tests;

public sealed class AggregateAndRuleTests
{
    private sealed record Created : DomainEvent;
    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id) { public void CreateEvent() => RaiseDomainEvent(new Created()); }
    private sealed class BrokenRule : IBusinessRule { public string Message => "Broken"; public bool IsBroken() => true; }
    [Fact] public void Aggregate_collects_and_clears_events() { TestAggregate a = new(Guid.NewGuid()); a.CreateEvent(); Assert.Single(a.DomainEvents); a.ClearDomainEvents(); Assert.Empty(a.DomainEvents); }
    [Fact] public void Broken_rule_throws() => Assert.Throws<BusinessRuleValidationException>(() => BusinessRule.Check(new BrokenRule()));
}
