namespace Diyarak.Platform.Domain.Primitives.Tests;

public sealed class AreaTests
{
    [Fact] public void Negative_area_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new Area(-1m, AreaUnit.SquareMeter));
    [Fact] public void Hectare_converts_to_square_meters() => Assert.Equal(10_000m, new Area(1m, AreaUnit.Hectare).ConvertTo(AreaUnit.SquareMeter).Value);
    [Fact] public void Dunum_converts_to_square_meters() => Assert.Equal(1_000m, new Area(1m, AreaUnit.Dunum).ConvertTo(AreaUnit.SquareMeter).Value);
    [Fact] public void Square_meters_round_trip() => Assert.Equal(125m, new Area(125m, AreaUnit.SquareMeter).ConvertTo(AreaUnit.SquareFoot).ConvertTo(AreaUnit.SquareMeter).Value, 8);
}
