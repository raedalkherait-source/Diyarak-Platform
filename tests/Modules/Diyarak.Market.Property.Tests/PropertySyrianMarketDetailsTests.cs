using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertySyrianMarketDetailsTests
{
    [Fact]
    public void Constructor_sets_legal_title_finishing_and_floor_details()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            legalTitleType: LegalTitleType.GreenTitleDeed,
            finishingLevel: FinishingLevel.SuperDeluxe,
            floorNumber: 3,
            buildingFloorCount: 7);

        Assert.Equal(LegalTitleType.GreenTitleDeed, property.LegalTitleType);
        Assert.Equal(FinishingLevel.SuperDeluxe, property.FinishingLevel);
        Assert.Equal(3, property.FloorNumber);
        Assert.Equal(7, property.BuildingFloorCount);
    }

    [Fact]
    public void Details_are_unknown_by_default()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress());

        Assert.Null(property.LegalTitleType);
        Assert.Null(property.FinishingLevel);
        Assert.Null(property.FloorNumber);
        Assert.Null(property.BuildingFloorCount);
    }

    [Fact]
    public void Basement_and_ground_floors_are_accepted()
    {
        var basement = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            floorNumber: -1);
        var ground = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            floorNumber: 0);

        Assert.Equal(-1, basement.FloorNumber);
        Assert.Equal(0, ground.FloorNumber);
    }

    [Theory]
    [InlineData(Property.MinimumFloorNumber - 1, null)]
    [InlineData(Property.MaximumFloorNumber + 1, null)]
    [InlineData(null, 0)]
    [InlineData(null, Property.MaximumFloorNumber + 1)]
    public void Constructor_rejects_out_of_range_floor_values(
        int? floorNumber,
        int? buildingFloorCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                floorNumber: floorNumber,
                buildingFloorCount: buildingFloorCount));
    }

    [Fact]
    public void Constructor_rejects_floor_above_building_floor_count()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                floorNumber: 8,
                buildingFloorCount: 5));
    }

    [Fact]
    public void Constructor_rejects_undefined_legal_title_and_finishing_level()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                legalTitleType: (LegalTitleType)99));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                finishingLevel: (FinishingLevel)99));
    }

    [Fact]
    public void ReplaceDetails_clears_omitted_details()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            legalTitleType: LegalTitleType.CourtRuling,
            finishingLevel: FinishingLevel.Deluxe,
            floorNumber: 2,
            buildingFloorCount: 4);

        property.ReplaceDetails(
            PropertyCategory.Apartment,
            CreateAddress());

        Assert.Null(property.LegalTitleType);
        Assert.Null(property.FinishingLevel);
        Assert.Null(property.FloorNumber);
        Assert.Null(property.BuildingFloorCount);
    }

    [Fact]
    public void Restore_rehydrates_details()
    {
        Property property = Property.Restore(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            livingArea: null,
            usableArea: null,
            totalRooms: null,
            bedroomCount: null,
            bathroomCount: null,
            furnishingQuality: null,
            features: [PropertyFeature.SolarPowerSystem],
            constructionYear: null,
            lastModernizationYear: null,
            commercialSubtype: null,
            salesArea: null,
            totalArea: null,
            parkingSpaceCount: null,
            ownerUserId: Guid.NewGuid(),
            version: 3,
            legalTitleType: LegalTitleType.TitleDeedShares,
            finishingLevel: FinishingLevel.Standard,
            floorNumber: 1,
            buildingFloorCount: 3);

        Assert.Equal(LegalTitleType.TitleDeedShares, property.LegalTitleType);
        Assert.Equal(FinishingLevel.Standard, property.FinishingLevel);
        Assert.Equal(1, property.FloorNumber);
        Assert.Equal(3, property.BuildingFloorCount);
        Assert.Contains(PropertyFeature.SolarPowerSystem, property.Features);
    }

    private static PropertyAddress CreateAddress() =>
        new(
            street: null,
            houseNumber: null,
            postalCode: null,
            city: "Damascus",
            governorate: SyrianGovernorate.Damascus,
            district: "Mezzeh");
}
