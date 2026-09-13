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

        var serviceProvider =
            ((Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>)context)
            .Instance;

        var designTimeModel =
            (Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel)
            serviceProvider.GetService(
                typeof(Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel))!;

        var model = designTimeModel.Model;

        var entityType = Assert.Single(
            model.GetEntityTypes(),
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
            "OwnerUserId",
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

        var ownerProperty = entityType.FindProperty("OwnerUserId");
        Assert.NotNull(ownerProperty);
        Assert.True(ownerProperty!.IsNullable);
        Assert.Equal(
            "owner_user_id",
            ownerProperty.GetColumnName());

        var ownerConstraint = Assert.Single(
            entityType.GetCheckConstraints(),
            constraint =>
                constraint.Name ==
                "ck_market_properties_owner_user_id_non_empty");

        Assert.Contains(
            "owner_user_id IS NULL",
            ownerConstraint.Sql);
    }
}


