using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void Constructor_sets_id_and_category()
    {
        Guid id = Guid.NewGuid();

        var property = new Property(id, PropertyCategory.Apartment);

        Assert.Equal(id, property.Id);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
    }

    [Fact]
    public void Constructor_rejects_empty_id()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(Guid.Empty, PropertyCategory.House));
    }

    [Fact]
    public void Constructor_rejects_invalid_category()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(Guid.NewGuid(), (PropertyCategory)0));
    }
}
