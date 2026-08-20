namespace Diyarak.Platform.SharedKernel.Tests;

public sealed class SpecificationTests
{
    private sealed class Positive : Specification<int> { public override bool IsSatisfiedBy(int candidate) => candidate > 0; }
    private sealed class Even : Specification<int> { public override bool IsSatisfiedBy(int candidate) => candidate % 2 == 0; }
    [Theory][InlineData(2, true)][InlineData(3, false)][InlineData(-2, false)] public void And_composes(int value, bool expected) => Assert.Equal(expected, new Positive().And(new Even()).IsSatisfiedBy(value));
    [Theory][InlineData(2, true)][InlineData(3, true)][InlineData(-3, false)] public void Or_composes(int value, bool expected) => Assert.Equal(expected, new Positive().Or(new Even()).IsSatisfiedBy(value));
    [Fact] public void Not_inverts() => Assert.True(new Positive().Not().IsSatisfiedBy(-1));
}
