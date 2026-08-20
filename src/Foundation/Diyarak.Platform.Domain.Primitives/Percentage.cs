namespace Diyarak.Platform.Domain.Primitives;

public readonly record struct Percentage
{
    public Percentage(decimal value)
    {
        if (value is < 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }
    public decimal Value { get; }
    public decimal AsFraction => Value / 100m;
    public override string ToString() => $"{Value}%";
}
