using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyBuildingDataTests
{
    [Fact]
    public void Constructor_sets_optional_construction_and_modernization_years()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress(),
            constructionYear: 1998,
            lastModernizationYear: 2021);

        Assert.Equal(1998, property.ConstructionYear);
        Assert.Equal(2021, property.LastModernizationYear);
    }

    [Fact]
    public void Constructor_leaves_building_years_unknown_by_default()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress());

        Assert.Null(property.ConstructionYear);
        Assert.Null(property.LastModernizationYear);
    }

    [Fact]
    public void Constructor_rejects_non_positive_building_years()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.House,
                CreateAddress(),
                constructionYear: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.House,
                CreateAddress(),
                lastModernizationYear: 0));
    }

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
