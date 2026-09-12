using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingModelTests
{
    [Fact]
    public void Model_contains_complete_market_listing_record()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_listing_model_test")
                .Options;

        using var context = new PlatformDbContext(options);

        var entityType = Assert.Single(
            context.Model.GetEntityTypes(),
            entity =>
                entity.ClrType.Name == "MarketListingRecord");

        Assert.Equal("market", entityType.GetSchema());
        Assert.Equal("listings", entityType.GetTableName());

        string[] expectedProperties =
        [
            "AvailableFromDate",
            "Headline",
            "Id",
            "PriceAmount",
            "PriceCurrency",
            "PriceIsOnRequest",
            "PublisherUserId",
            "PublishingRole",
            "Status",
            "SubjectId",
            "SubjectType",
            "TransactionIntent",
            "Version",
        ];

        string[] actualProperties =
            entityType
                .GetProperties()
                .Select(property => property.Name)
                .Order()
                .ToArray();

        Assert.Equal(
            expectedProperties.Order(),
            actualProperties);

        var primaryKey = Assert.Single(
            entityType.FindPrimaryKey()!.Properties);

        Assert.Equal("Id", primaryKey.Name);

        var statusProperty = entityType.FindProperty("Status");
        var versionProperty = entityType.FindProperty("Version");

        Assert.NotNull(statusProperty);
        Assert.False(statusProperty!.IsConcurrencyToken);
        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }
}
