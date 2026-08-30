using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class CommercialPropertySubtypeTests
{
    [Fact]
    public void Constructor_sets_optional_commercial_subtype()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress(),
            commercialSubtype: CommercialPropertySubtype.Retail);

        Assert.Equal(CommercialPropertySubtype.Retail, property.CommercialSubtype);
    }

    [Fact]
    public void Constructor_leaves_commercial_subtype_unspecified_by_default()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            CreateAddress());

        Assert.Null(property.CommercialSubtype);
    }

    [Fact]
    public void Constructor_rejects_commercial_subtype_for_non_commercial_property()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                commercialSubtype: CommercialPropertySubtype.OfficeOrPractice));
    }

    [Fact]
    public void Constructor_rejects_invalid_commercial_subtype()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.CommercialProperty,
                CreateAddress(),
                commercialSubtype: (CommercialPropertySubtype)0));
    }

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
