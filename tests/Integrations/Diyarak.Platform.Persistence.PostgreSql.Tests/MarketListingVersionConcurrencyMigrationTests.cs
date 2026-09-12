using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingVersionConcurrencyMigrationTests
{
    [Fact]
    public void Up_adds_backfills_and_requires_listing_version()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        Assert.Collection(
            operations,
            operation =>
            {
                var column = Assert.IsType<AddColumnOperation>(operation);
                Assert.Equal("version", column.Name);
                Assert.Equal("market", column.Schema);
                Assert.Equal("listings", column.Table);
                Assert.Equal("bigint", column.ColumnType);
                Assert.True(column.IsNullable);
            },
            operation =>
            {
                var sqlOperation = Assert.IsType<SqlOperation>(operation);
                Assert.Contains(
                    "UPDATE market.listings SET version = 1",
                    sqlOperation.Sql);
            },
            operation =>
            {
                var column = Assert.IsType<AlterColumnOperation>(operation);
                Assert.Equal("version", column.Name);
                Assert.Equal("market", column.Schema);
                Assert.Equal("listings", column.Table);
                Assert.Equal("bigint", column.ColumnType);
                Assert.False(column.IsNullable);
                Assert.Null(column.DefaultValue);
                Assert.Null(column.DefaultValueSql);
            });
    }

    private sealed class TestMigration
        : MarketListingVersionConcurrency
    {
        internal List<MigrationOperation> BuildUpOperations()
        {
            var builder =
                new MigrationBuilder(
                    "Npgsql.EntityFrameworkCore.PostgreSQL");

            Up(builder);

            return builder.Operations;
        }
    }
}
