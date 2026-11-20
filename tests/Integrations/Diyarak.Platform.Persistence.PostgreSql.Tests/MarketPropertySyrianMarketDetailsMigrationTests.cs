using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketPropertySyrianMarketDetailsMigrationTests
{
    [Fact]
    public void Up_relaxes_street_address_parts_and_adds_nullable_details()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        string[] relaxed =
        [
            .. operations
                .OfType<AlterColumnOperation>()
                .Where(static column => column.Table == "properties" && column.Schema == "market")
                .Select(static column =>
                {
                    Assert.True(column.IsNullable);
                    return column.Name;
                })
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(["house_number", "postal_code", "street"], relaxed);

        AddColumnOperation[] added =
        [
            .. operations.OfType<AddColumnOperation>(),
        ];

        Assert.Equal(
            ["building_floor_count", "district", "finishing_level", "floor_number", "governorate", "legal_title_type"],
            added.Select(static column => column.Name).Order(StringComparer.Ordinal));

        Assert.All(
            added,
            static column =>
            {
                Assert.Equal("market", column.Schema);
                Assert.Equal("properties", column.Table);
                Assert.True(column.IsNullable);
                Assert.Null(column.DefaultValue);
            });

        Assert.DoesNotContain(operations, static operation => operation is SqlOperation);
    }

    [Fact]
    public void Down_backfills_empty_strings_before_restoring_not_null()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildDownOperations();

        int backfillIndex = operations.FindIndex(static operation => operation is SqlOperation);
        int firstAlterIndex = operations.FindIndex(static operation => operation is AlterColumnOperation);

        Assert.True(backfillIndex >= 0);
        Assert.True(backfillIndex < firstAlterIndex);
        Assert.Contains(
            "COALESCE(street, '')",
            ((SqlOperation)operations[backfillIndex]).Sql,
            StringComparison.Ordinal);
    }

    private sealed class TestMigration
        : MarketPropertySyrianMarketDetails
    {
        internal List<MigrationOperation> BuildUpOperations()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return builder.Operations;
        }

        internal List<MigrationOperation> BuildDownOperations()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Down(builder);
            return builder.Operations;
        }
    }
}
