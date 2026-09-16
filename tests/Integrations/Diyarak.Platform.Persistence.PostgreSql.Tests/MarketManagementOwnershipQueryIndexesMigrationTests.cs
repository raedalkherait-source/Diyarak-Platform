using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketManagementOwnershipQueryIndexesMigrationTests
{
    [Fact]
    public void Up_creates_management_ownership_query_indexes()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        Assert.Collection(
            operations,
            operation =>
            {
                var index =
                    Assert.IsType<CreateIndexOperation>(
                        operation);

                Assert.Equal(
                    "ix_market_listings_publisher_user_id",
                    index.Name);
                Assert.Equal("market", index.Schema);
                Assert.Equal("listings", index.Table);
                Assert.Collection(
                    index.Columns,
                    column =>
                        Assert.Equal(
                            "publisher_user_id",
                            column));
                Assert.False(index.IsUnique);
            },
            operation =>
            {
                var index =
                    Assert.IsType<CreateIndexOperation>(
                        operation);

                Assert.Equal(
                    "ix_market_properties_owner_user_id",
                    index.Name);
                Assert.Equal("market", index.Schema);
                Assert.Equal("properties", index.Table);
                Assert.Collection(
                    index.Columns,
                    column =>
                        Assert.Equal(
                            "owner_user_id",
                            column));
                Assert.False(index.IsUnique);
            });
    }

    [Fact]
    public void Down_removes_management_ownership_query_indexes()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildDownOperations();

        Assert.Collection(
            operations,
            operation =>
            {
                var index =
                    Assert.IsType<DropIndexOperation>(
                        operation);

                Assert.Equal(
                    "ix_market_properties_owner_user_id",
                    index.Name);
                Assert.Equal("market", index.Schema);
                Assert.Equal("properties", index.Table);
            },
            operation =>
            {
                var index =
                    Assert.IsType<DropIndexOperation>(
                        operation);

                Assert.Equal(
                    "ix_market_listings_publisher_user_id",
                    index.Name);
                Assert.Equal("market", index.Schema);
                Assert.Equal("listings", index.Table);
            });
    }

    private sealed class TestMigration
        : MarketManagementOwnershipQueryIndexes
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
