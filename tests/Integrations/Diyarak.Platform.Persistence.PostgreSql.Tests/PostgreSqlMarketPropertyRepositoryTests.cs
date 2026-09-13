using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlMarketPropertyRepositoryTests
{
    [Fact]
    public async Task AddAsync_persists_complete_property_state()
    {
        await using PlatformDbContext context = CreateContext();

        Guid ownerUserId = Guid.NewGuid();
        var property = new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.CommercialProperty,
            new PropertyAddress(
                "Harbor Road",
                "5",
                "20457",
                "Hamburg",
                new GeoCoordinate(53.5439, 9.9846)),
            livingArea: new Area(15m, AreaUnit.SquareMeter),
            usableArea: new Area(120m, AreaUnit.SquareMeter),
            totalRooms: 4m,
            bedroomCount: 1,
            bathroomCount: 2,
            furnishingQuality: FurnishingQuality.Normal,
            features:
            [
                PropertyFeature.Elevator,
                PropertyFeature.StepFreeAccess,
            ],
            constructionYear: 2005,
            lastModernizationYear: 2025,
            commercialSubtype: CommercialPropertySubtype.OfficeOrPractice,
            salesArea: new Area(75m, AreaUnit.SquareMeter),
            totalArea: new Area(140m, AreaUnit.SquareMeter),
            parkingSpaceCount: 3,
            ownerUserId: ownerUserId);

        var repository =
            new PostgreSqlMarketPropertyRepository(context);

        await repository.AddAsync(property);

        context.ChangeTracker.Clear();

        MarketPropertyRecord persisted =
            await context.MarketProperties.SingleAsync(
                record => record.Id == property.Id);

        Assert.Equal(property.Id, persisted.Id);
        Assert.Equal(ownerUserId, persisted.OwnerUserId);
        Assert.Equal((int)PropertyCategory.CommercialProperty, persisted.Category);
        Assert.Equal("Harbor Road", persisted.Street);
        Assert.Equal("5", persisted.HouseNumber);
        Assert.Equal("20457", persisted.PostalCode);
        Assert.Equal("Hamburg", persisted.City);
        Assert.Equal(53.5439, persisted.Latitude);
        Assert.Equal(9.9846, persisted.Longitude);
        Assert.Equal(15m, persisted.LivingAreaValue);
        Assert.Equal((int)AreaUnit.SquareMeter, persisted.LivingAreaUnit);
        Assert.Equal(120m, persisted.UsableAreaValue);
        Assert.Equal(4m, persisted.TotalRooms);
        Assert.Equal(1, persisted.BedroomCount);
        Assert.Equal(2, persisted.BathroomCount);
        Assert.Equal((int)FurnishingQuality.Normal, persisted.FurnishingQuality);
        Assert.Equal(
            [(int)PropertyFeature.Elevator, (int)PropertyFeature.StepFreeAccess],
            persisted.Features);
        Assert.Equal(2005, persisted.ConstructionYear);
        Assert.Equal(2025, persisted.LastModernizationYear);
        Assert.Equal(
            (int)CommercialPropertySubtype.OfficeOrPractice,
            persisted.CommercialSubtype);
        Assert.Equal(75m, persisted.SalesAreaValue);
        Assert.Equal(140m, persisted.TotalAreaValue);
        Assert.Equal(3, persisted.ParkingSpaceCount);
    }


    [Fact]
    public async Task FindByIdAsync_returns_complete_property_state_without_tracking()
    {
        await using PlatformDbContext context = CreateContext();

        Guid ownerUserId = Guid.NewGuid();
        var property = new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            new PropertyAddress(
                "River Street",
                "18",
                "28195",
                "Bremen",
                new GeoCoordinate(53.0793, 8.8017)),
            livingArea: new Area(74m, AreaUnit.SquareMeter),
            usableArea: new Area(80m, AreaUnit.SquareMeter),
            totalRooms: 3m,
            bedroomCount: 2,
            bathroomCount: 1,
            furnishingQuality: FurnishingQuality.Upscale,
            features:
            [
                PropertyFeature.FittedKitchen,
                PropertyFeature.BalconyOrTerrace,
            ],
            constructionYear: 2010,
            lastModernizationYear: 2023,
            parkingSpaceCount: 1,
            ownerUserId: ownerUserId);

        var repository =
            new PostgreSqlMarketPropertyRepository(context);

        await repository.AddAsync(property);
        context.ChangeTracker.Clear();

        MarketProperty? loaded =
            await repository.FindByIdAsync(property.Id);

        MarketProperty existing = Assert.IsType<MarketProperty>(loaded);
        Assert.Equal(property.Id, existing.Id);
        Assert.Equal(ownerUserId, existing.OwnerUserId);
        Assert.Equal(PropertyCategory.Apartment, existing.Category);
        Assert.Equal("River Street", existing.Address.Street);
        Assert.Equal("Bremen", existing.Address.City);
        Assert.Equal(53.0793, existing.Address.Location?.Latitude);
        Assert.Equal(74m, existing.LivingArea?.Value);
        Assert.Equal(3m, existing.TotalRooms);
        Assert.Equal(FurnishingQuality.Upscale, existing.FurnishingQuality);
        Assert.Contains(
            PropertyFeature.BalconyOrTerrace,
            existing.Features);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_property_does_not_exist()
    {
        await using PlatformDbContext context = CreateContext();
        var repository =
            new PostgreSqlMarketPropertyRepository(context);

        MarketProperty? loaded =
            await repository.FindByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }
}
