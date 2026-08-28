using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyAddressTests
{
    [Fact]
    public void Constructor_sets_address_components_and_location()
    {
        var location = new GeoCoordinate(52.52, 13.405);

        var address = new PropertyAddress(
            "Example Street",
            "12A",
            "12345",
            "Example City",
            location);

        Assert.Equal("Example Street", address.Street);
        Assert.Equal("12A", address.HouseNumber);
        Assert.Equal("12345", address.PostalCode);
        Assert.Equal("Example City", address.City);
        Assert.Equal(location, address.Location);
    }

    [Fact]
    public void Addresses_with_the_same_components_are_equal()
    {
        var first = new PropertyAddress(
            "Example Street",
            "12A",
            "12345",
            "Example City");

        var second = new PropertyAddress(
            "Example Street",
            "12A",
            "12345",
            "Example City");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_rejects_blank_required_components()
    {
        Assert.Throws<ArgumentException>(
            () => new PropertyAddress("", "12A", "12345", "Example City"));

        Assert.Throws<ArgumentException>(
            () => new PropertyAddress("Example Street", " ", "12345", "Example City"));

        Assert.Throws<ArgumentException>(
            () => new PropertyAddress("Example Street", "12A", "", "Example City"));

        Assert.Throws<ArgumentException>(
            () => new PropertyAddress("Example Street", "12A", "12345", " "));
    }
}
