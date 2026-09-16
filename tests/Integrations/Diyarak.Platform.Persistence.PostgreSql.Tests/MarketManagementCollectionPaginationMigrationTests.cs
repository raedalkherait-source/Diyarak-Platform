using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketManagementCollectionPaginationMigrationTests
{
    private static readonly string[] ListingCompositeColumns =
    [
        "publisher_user_id",
        "id",
    ];

    private static readonly string[] PropertyCompositeColumns =
    [
        "owner_user_id",
        "id",
    ];

    private static readonly string[] ListingSingleColumns =
    [
        "publisher_user_id",
    ];

    private static readonly string[] PropertySingleColumns =
    [
        "owner_user_id",
    ];

    [Fact]
    public void Up_replaces_single_column_indexes_with_composite_pagination_indexes()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        Assert.Collection(
            operations,
            operation => AssertDrop(operation, "ix_market_listings_publisher_user_id", "listings"),
            operation => AssertDrop(operation, "ix_market_properties_owner_user_id", "properties"),
            operation => AssertCreate(
                operation,
                "ix_market_listings_publisher_user_id",
                "listings",
                ListingCompositeColumns),
            operation => AssertCreate(
                operation,
                "ix_market_properties_owner_user_id",
                "properties",
                PropertyCompositeColumns));
    }

    [Fact]
    public void Down_restores_single_column_ownership_indexes()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildDownOperations();

        Assert.Collection(
            operations,
            operation => AssertDrop(operation, "ix_market_listings_publisher_user_id", "listings"),
            operation => AssertDrop(operation, "ix_market_properties_owner_user_id", "properties"),
            operation => AssertCreate(
                operation,
                "ix_market_listings_publisher_user_id",
                "listings",
                ListingSingleColumns),
            operation => AssertCreate(
                operation,
                "ix_market_properties_owner_user_id",
                "properties",
                PropertySingleColumns));
    }

    private static void AssertDrop(
        MigrationOperation operation,
        string name,
        string table)
    {
        var index = Assert.IsType<DropIndexOperation>(operation);
        Assert.Equal(name, index.Name);
        Assert.Equal("market", index.Schema);
        Assert.Equal(table, index.Table);
    }

    private static void AssertCreate(
        MigrationOperation operation,
        string name,
        string table,
        IReadOnlyList<string> columns)
    {
        var index = Assert.IsType<CreateIndexOperation>(operation);
        Assert.Equal(name, index.Name);
        Assert.Equal("market", index.Schema);
        Assert.Equal(table, index.Table);
        Assert.Equal(columns, index.Columns);
        Assert.False(index.IsUnique);
    }

    private sealed class TestMigration
        : MarketManagementCollectionPagination
    {
        internal List<MigrationOperation> BuildUpOperations()
        {
            var builder = new MigrationBuilder(
                "Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return builder.Operations;
        }

        internal List<MigrationOperation> BuildDownOperations()
        {
            var builder = new MigrationBuilder(
                "Npgsql.EntityFrameworkCore.PostgreSQL");
            Down(builder);
            return builder.Operations;
        }
    }
}
