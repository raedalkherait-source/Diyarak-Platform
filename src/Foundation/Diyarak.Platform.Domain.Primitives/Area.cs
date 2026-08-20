namespace Diyarak.Platform.Domain.Primitives;

public readonly record struct Area
{
    private const decimal SquareFootInSquareMeters = 0.09290304m;
    private const decimal HectareInSquareMeters = 10_000m;
    private const decimal DunumInSquareMeters = 1_000m;

    public Area(decimal value, AreaUnit unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (!Enum.IsDefined(unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        Value = value;
        Unit = unit;
    }

    public decimal Value { get; }
    public AreaUnit Unit { get; }

    public Area ConvertTo(AreaUnit target)
    {
        if (!Enum.IsDefined(target)) throw new ArgumentOutOfRangeException(nameof(target));
        decimal squareMeters = Unit switch
        {
            AreaUnit.SquareMeter => Value,
            AreaUnit.SquareFoot => Value * SquareFootInSquareMeters,
            AreaUnit.Hectare => Value * HectareInSquareMeters,
            AreaUnit.Dunum => Value * DunumInSquareMeters,
            _ => throw new InvalidOperationException("Unsupported area unit."),
        };
        decimal converted = target switch
        {
            AreaUnit.SquareMeter => squareMeters,
            AreaUnit.SquareFoot => squareMeters / SquareFootInSquareMeters,
            AreaUnit.Hectare => squareMeters / HectareInSquareMeters,
            AreaUnit.Dunum => squareMeters / DunumInSquareMeters,
            _ => throw new InvalidOperationException("Unsupported area unit."),
        };
        return new Area(converted, target);
    }

    public override string ToString() => $"{Value} {Unit}";
}
