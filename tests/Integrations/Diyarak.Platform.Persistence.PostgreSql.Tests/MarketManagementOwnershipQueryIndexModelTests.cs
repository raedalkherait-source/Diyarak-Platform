using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketManagementOwnershipQueryIndexModelTests
{
    [Fact]
    public void Model_contains_management_ownership_query_indexes()
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

        var listingType = Assert.Single(
            model.GetEntityTypes(),
            candidate =>
                candidate.ClrType.Name ==
                "MarketListingRecord");

        var listingIndex = Assert.Single(
            listingType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_market_listings_publisher_user_id");

        Assert.Collection(
            listingIndex.Properties,
            property =>
                Assert.Equal(
                    "PublisherUserId",
                    property.Name));

        var propertyType = Assert.Single(
            model.GetEntityTypes(),
            candidate =>
                candidate.ClrType.Name ==
                "MarketPropertyRecord");

        var propertyIndex = Assert.Single(
            propertyType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_market_properties_owner_user_id");

        Assert.Collection(
            propertyIndex.Properties,
            property =>
                Assert.Equal(
                    "OwnerUserId",
                    property.Name));
    }
}
