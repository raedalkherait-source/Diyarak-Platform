using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PlatformDbContextModelTests
{
    [Fact]
    public void Model_contains_complete_market_property_record()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_model_test")
                .Options;

        using var context = new PlatformDbContext(options);

        var entityType = Assert.Single(
            context.Model.GetEntityTypes(),
            candidate =>
                candidate.ClrType.Name ==
                "MarketPropertyRecord");

        Assert.Equal("market", entityType.GetSchema());
        Assert.Equal("properties", entityType.GetTableName());

        string[] expectedProperties =
        [
            "Id",
            "Category",
            "Street",
            "HouseNumber",
            "PostalCode",
            "City",
            "Latitude",
            "Longitude",
            "LivingAreaValue",
            "LivingAreaUnit",
            "UsableAreaValue",
            "UsableAreaUnit",
            "TotalRooms",
            "BedroomCount",
            "BathroomCount",
            "FurnishingQuality",
            "Features",
            "ConstructionYear",
            "LastModernizationYear",
            "CommercialSubtype",
            "SalesAreaValue",
            "SalesAreaUnit",
            "TotalAreaValue",
            "TotalAreaUnit",
            "ParkingSpaceCount",
        ];

        string[] actualProperties =
        [
            .. entityType
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(
            expectedProperties.Order(StringComparer.Ordinal),
            actualProperties);

        var primaryKey = Assert.Single(
            entityType.GetKeys(),
            key => key.IsPrimaryKey());

        Assert.Equal(
            ["Id"],
            primaryKey.Properties.Select(
                property => property.Name));
    }
}
