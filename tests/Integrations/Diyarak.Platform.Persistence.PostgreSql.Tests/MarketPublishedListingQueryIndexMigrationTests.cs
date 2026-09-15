using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketPublishedListingQueryIndexMigrationTests
{
    [Fact]
    public void Up_creates_status_identifier_index_for_public_listing_collection()
    {
        MigrationOperation operation = Assert.Single(
            new TestMigration().BuildUpOperations());

        var index = Assert.IsType<CreateIndexOperation>(operation);

        Assert.Equal(
            "ix_market_listings_status_id",
            index.Name);
        Assert.Equal("market", index.Schema);
        Assert.Equal("listings", index.Table);
        Assert.Collection(
            index.Columns,
            column => Assert.Equal("status", column),
            column => Assert.Equal("id", column));
        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Down_removes_public_listing_collection_index()
    {
        MigrationOperation operation = Assert.Single(
            new TestMigration().BuildDownOperations());

        var index = Assert.IsType<DropIndexOperation>(operation);

        Assert.Equal(
            "ix_market_listings_status_id",
            index.Name);
        Assert.Equal("market", index.Schema);
        Assert.Equal("listings", index.Table);
    }

    private sealed class TestMigration
        : MarketPublishedListingQueryIndex
    {
        internal List<MigrationOperation> BuildUpOperations()
        {
            var builder =
                new MigrationBuilder(
                    "Npgsql.EntityFrameworkCore.PostgreSQL");

            Up(builder);

            return builder.Operations;
        }

        internal List<MigrationOperation> BuildDownOperations()
        {
            var builder =
                new MigrationBuilder(
                    "Npgsql.EntityFrameworkCore.PostgreSQL");

            Down(builder);

            return builder.Operations;
        }
    }
}
