using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketPropertyRecordMapperTests
{
    [Fact]
    public void Round_trip_preserves_syrian_market_details()
    {
        var original = new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            new PropertyAddress(
                street: null,
                houseNumber: null,
                postalCode: null,
                city: "Damascus",
                location: new GeoCoordinate(33.5138d, 36.2765d),
                governorate: SyrianGovernorate.Damascus,
                district: "Mezzeh"),
            features: [PropertyFeature.SolarPowerSystem, PropertyFeature.GeneratorSubscription],
            ownerUserId: Guid.NewGuid(),
            legalTitleType: LegalTitleType.GreenTitleDeed,
            finishingLevel: FinishingLevel.SuperDeluxe,
            floorNumber: -1,
            buildingFloorCount: 6);

        var record = MarketPropertyRecordMapper.FromDomain(original);
        var restored = MarketPropertyRecordMapper.ToDomain(record);

        Assert.Null(record.Street);
        Assert.Null(record.PostalCode);
        Assert.Equal((int)SyrianGovernorate.Damascus, record.Governorate);
        Assert.Equal(original.Address, restored.Address);
        Assert.Equal(LegalTitleType.GreenTitleDeed, restored.LegalTitleType);
        Assert.Equal(FinishingLevel.SuperDeluxe, restored.FinishingLevel);
        Assert.Equal(-1, restored.FloorNumber);
        Assert.Equal(6, restored.BuildingFloorCount);
        Assert.Equal(original.Features.Order(), restored.Features.Order());
    }

    [Fact]
    public void Round_trip_preserves_complete_market_property_state()
    {
        Guid ownerUserId = Guid.NewGuid();
        var original = new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            new PropertyAddress(
                "Market Street",
                "42",
                "10115",
                "Berlin",
                new GeoCoordinate(52.520008d, 13.404954d)),
            livingArea: new Area(120.5m, AreaUnit.SquareMeter),
            usableArea: new Area(135.75m, AreaUnit.SquareMeter),
            totalRooms: 5.5m,
            bedroomCount: 4,
            bathroomCount: 2,
            furnishingQuality: FurnishingQuality.Luxury,
            features:
            [
                PropertyFeature.FittedKitchen,
                PropertyFeature.Elevator,
                PropertyFeature.BalconyOrTerrace,
            ],
            constructionYear: 1998,
            lastModernizationYear: 2024,
            commercialSubtype:
                CommercialPropertySubtype.OfficeOrPractice,
            salesArea: new Area(150m, AreaUnit.SquareMeter),
            totalArea: new Area(180m, AreaUnit.SquareMeter),
            parkingSpaceCount: 3,
            ownerUserId: ownerUserId);

        var record =
            MarketPropertyRecordMapper.FromDomain(original);

        var restored =
            MarketPropertyRecordMapper.ToDomain(record);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(ownerUserId, record.OwnerUserId);
        Assert.Equal(ownerUserId, restored.OwnerUserId);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.Address, restored.Address);
        Assert.Equal(original.LivingArea, restored.LivingArea);
        Assert.Equal(original.UsableArea, restored.UsableArea);
        Assert.Equal(original.TotalRooms, restored.TotalRooms);
        Assert.Equal(original.BedroomCount, restored.BedroomCount);
        Assert.Equal(original.BathroomCount, restored.BathroomCount);
        Assert.Equal(
            original.FurnishingQuality,
            restored.FurnishingQuality);
        Assert.Equal(
            original.Features.Order(),
            restored.Features.Order());
        Assert.Equal(
            original.ConstructionYear,
            restored.ConstructionYear);
        Assert.Equal(
            original.LastModernizationYear,
            restored.LastModernizationYear);
        Assert.Equal(
            original.CommercialSubtype,
            restored.CommercialSubtype);
        Assert.Equal(original.SalesArea, restored.SalesArea);
        Assert.Equal(original.TotalArea, restored.TotalArea);
        Assert.Equal(
            original.ParkingSpaceCount,
            restored.ParkingSpaceCount);
    }

    [Fact]
    public void Round_trip_preserves_legacy_unowned_property_state()
    {
        var original = new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.House,
            new PropertyAddress(
                "Legacy Road",
                "1",
                "20095",
                "Hamburg"));

        MarketPropertyRecord record =
            MarketPropertyRecordMapper.FromDomain(original);

        MarketProperty restored =
            MarketPropertyRecordMapper.ToDomain(record);

        Assert.Null(record.OwnerUserId);
        Assert.Null(restored.OwnerUserId);
    }
}
