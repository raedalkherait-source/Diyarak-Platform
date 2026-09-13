using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketPropertyOwnershipMigrationTests
{
    [Fact]
    public void Up_adds_nullable_owner_without_default_or_legacy_backfill()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        var addColumn = Assert.Single(
            operations.OfType<AddColumnOperation>());

        Assert.Equal("owner_user_id", addColumn.Name);
        Assert.Equal("market", addColumn.Schema);
        Assert.Equal("properties", addColumn.Table);
        Assert.True(addColumn.IsNullable);
        Assert.Null(addColumn.DefaultValue);
        Assert.Null(addColumn.DefaultValueSql);

        Assert.Empty(operations.OfType<SqlOperation>());

        var checkConstraint = Assert.Single(
            operations.OfType<AddCheckConstraintOperation>());

        Assert.Equal(
            "ck_market_properties_owner_user_id_non_empty",
            checkConstraint.Name);
        Assert.Contains(
            "owner_user_id IS NULL",
            checkConstraint.Sql);
        Assert.Contains(
            "00000000-0000-0000-0000-000000000000",
            checkConstraint.Sql);
    }

    private sealed class TestMigration
        : MarketPropertyOwnership
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
