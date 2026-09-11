using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingPublisherOwnershipMigrationTests
{
    [Fact]
    public void Up_rejects_legacy_rows_and_adds_required_owner_without_default()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        Assert.Collection(
            operations,
            operation =>
            {
                var sqlOperation =
                    Assert.IsType<SqlOperation>(operation);

                Assert.Contains(
                    "IF EXISTS",
                    sqlOperation.Sql);

                Assert.Contains(
                    "market.listings",
                    sqlOperation.Sql);
            },
            operation =>
            {
                var column =
                    Assert.IsType<AddColumnOperation>(operation);

                Assert.Equal(
                    "publisher_user_id",
                    column.Name);
                Assert.Equal("market", column.Schema);
                Assert.Equal("listings", column.Table);
                Assert.Equal("uuid", column.ColumnType);
                Assert.False(column.IsNullable);
                Assert.Null(column.DefaultValue);
                Assert.Null(column.DefaultValueSql);
            });
    }

    private sealed class TestMigration
        : MarketListingPublisherOwnership
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
