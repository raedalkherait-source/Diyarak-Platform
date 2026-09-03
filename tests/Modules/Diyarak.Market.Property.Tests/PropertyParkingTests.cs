using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyParkingTests
{
    [Fact]
    public void Constructor_sets_optional_parking_space_count()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress(),
            parkingSpaceCount: 6);

        Assert.Equal(6, property.ParkingSpaceCount);
    }

    [Fact]
    public void Constructor_leaves_parking_space_count_unknown_by_default()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress());

        Assert.Null(property.ParkingSpaceCount);
    }

    [Fact]
    public void Constructor_rejects_negative_parking_space_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.CommercialProperty,
                CreateAddress(),
                parkingSpaceCount: -1));
    }

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
