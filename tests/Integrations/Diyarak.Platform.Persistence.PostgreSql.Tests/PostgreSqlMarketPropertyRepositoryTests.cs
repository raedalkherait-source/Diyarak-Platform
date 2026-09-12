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
            parkingSpaceCount: 3);

        var repository =
            new PostgreSqlMarketPropertyRepository(context);

        await repository.AddAsync(property);

        context.ChangeTracker.Clear();

        MarketPropertyRecord persisted =
            await context.MarketProperties.SingleAsync(
                record => record.Id == property.Id);

        Assert.Equal(property.Id, persisted.Id);
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

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }
}
