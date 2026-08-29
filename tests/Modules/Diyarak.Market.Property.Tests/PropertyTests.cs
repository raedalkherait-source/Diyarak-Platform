using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void Constructor_sets_id_category_and_address()
    {
        Guid id = Guid.NewGuid();
        var address = CreateAddress();

        var property = new Property(id, PropertyCategory.Apartment, address);

        Assert.Equal(id, property.Id);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal(address, property.Address);
        Assert.Null(property.LivingArea);
        Assert.Null(property.UsableArea);
        Assert.Null(property.TotalRooms);
        Assert.Null(property.BedroomCount);
        Assert.Null(property.BathroomCount);
    }

    [Fact]
    public void Constructor_sets_optional_areas()
    {
        var livingArea = new Area(120m, AreaUnit.SquareMeter);
        var usableArea = new Area(145m, AreaUnit.SquareMeter);

        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress(),
            livingArea,
            usableArea);

        Assert.Equal(livingArea, property.LivingArea);
        Assert.Equal(usableArea, property.UsableArea);
    }

    [Fact]
    public void Constructor_sets_optional_room_counts()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            totalRooms: 4m,
            bedroomCount: 3,
            bathroomCount: 2);

        Assert.Equal(4m, property.TotalRooms);
        Assert.Equal(3, property.BedroomCount);
        Assert.Equal(2, property.BathroomCount);
    }

    [Fact]
    public void Constructor_rejects_empty_id()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(Guid.Empty, PropertyCategory.House, CreateAddress()));
    }

    [Fact]
    public void Constructor_rejects_invalid_category()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(Guid.NewGuid(), (PropertyCategory)0, CreateAddress()));
    }

    [Fact]
    public void Constructor_rejects_null_address()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Property(Guid.NewGuid(), PropertyCategory.Apartment, null!));
    }


    [Fact]
    public void Constructor_rejects_negative_room_counts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                totalRooms: -1m));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                bedroomCount: -1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                bathroomCount: -1));
    }
    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
