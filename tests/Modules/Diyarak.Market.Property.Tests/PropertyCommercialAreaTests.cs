using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyCommercialAreaTests
{
    [Fact]
    public void Constructor_sets_optional_sales_and_total_areas()
    {
        var salesArea = new Area(129.1m, AreaUnit.SquareMeter);
        var totalArea = new Area(180m, AreaUnit.SquareMeter);

        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress(),
            salesArea: salesArea,
            totalArea: totalArea);

        Assert.Equal(salesArea, property.SalesArea);
        Assert.Equal(totalArea, property.TotalArea);
    }

    [Fact]
    public void Constructor_leaves_sales_and_total_areas_unknown_by_default()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress());

        Assert.Null(property.SalesArea);
        Assert.Null(property.TotalArea);
    }

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
