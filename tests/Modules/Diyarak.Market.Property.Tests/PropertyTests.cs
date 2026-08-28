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

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
