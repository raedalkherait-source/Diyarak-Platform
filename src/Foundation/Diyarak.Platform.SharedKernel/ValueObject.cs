namespace Diyarak.Platform.SharedKernel;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    public bool Equals(ValueObject? other) => other is not null && GetType() == other.GetType() && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (object? component in GetEqualityComponents()) hash.Add(component);
        return hash.ToHashCode();
    }
}
