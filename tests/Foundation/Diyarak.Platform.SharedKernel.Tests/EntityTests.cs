namespace Diyarak.Platform.SharedKernel.Tests;

public sealed class EntityTests
{
    private sealed class TestEntity(Guid id) : Entity<Guid>(id);
    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);
    [Fact] public void Same_type_and_id_are_equal() { Guid id = Guid.NewGuid(); Assert.Equal(new TestEntity(id), new TestEntity(id)); }
    [Fact] public void Different_types_with_same_id_are_not_equal() { Guid id = Guid.NewGuid(); Assert.NotEqual<object>(new TestEntity(id), new OtherEntity(id)); }
    [Fact] public void Default_id_is_rejected() => Assert.Throws<ArgumentException>(() => new TestEntity(Guid.Empty));
}
