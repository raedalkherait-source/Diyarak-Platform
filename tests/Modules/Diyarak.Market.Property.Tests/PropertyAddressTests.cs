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
    public void Constructor_rejects_blank_supplied_components()
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

    [Fact]
    public void Constructor_accepts_syrian_address_without_street_or_postal_code()
    {
        var address = new PropertyAddress(
            street: null,
            houseNumber: null,
            postalCode: null,
            city: "دمشق",
            governorate: SyrianGovernorate.Damascus,
            district: "المزة");

        Assert.Equal(SyrianGovernorate.Damascus, address.Governorate);
        Assert.Equal("دمشق", address.City);
        Assert.Equal("المزة", address.District);
        Assert.Null(address.Street);
        Assert.Null(address.HouseNumber);
        Assert.Null(address.PostalCode);
    }

    [Fact]
    public void Constructor_rejects_blank_district_and_undefined_governorate()
    {
        Assert.Throws<ArgumentException>(
            () => new PropertyAddress(null, null, null, "Aleppo", district: " "));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PropertyAddress(null, null, null, "Aleppo", governorate: (SyrianGovernorate)99));
    }

    [Fact]
    public void Addresses_with_different_district_are_not_equal()
    {
        var first = new PropertyAddress(null, null, null, "Damascus", district: "Mezzeh");
        var second = new PropertyAddress(null, null, null, "Damascus", district: "Malki");

        Assert.NotEqual(first, second);
    }
}
