using Diyarak.Platform.Persistence.PostgreSql.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class ExternalIdentityMappingsMigrationTests
{
    [Fact]
    public void Up_creates_identity_mapping_with_composite_key_and_non_empty_user_constraint()
    {
        List<MigrationOperation> operations =
            new TestMigration().BuildUpOperations();

        var createTable = Assert.Single(
            operations.OfType<CreateTableOperation>());

        Assert.Equal(
            "external_identity_mappings",
            createTable.Name);
        Assert.Equal("identity", createTable.Schema);
        Assert.Equal(
            ["issuer", "subject", "user_id"],
            createTable.Columns.Select(column => column.Name));

        Assert.NotNull(createTable.PrimaryKey);
        Assert.Equal(
            ["issuer", "subject"],
            createTable.PrimaryKey!.Columns);

        var checkConstraint = Assert.Single(
            createTable.CheckConstraints);

        Assert.Equal(
            "ck_external_identity_mappings_user_id_non_empty",
            checkConstraint.Name);
        Assert.Contains(
            "00000000-0000-0000-0000-000000000000",
            checkConstraint.Sql);

        var index = Assert.Single(
            operations.OfType<CreateIndexOperation>());

        Assert.Equal(
            "ix_external_identity_mappings_user_id",
            index.Name);
        Assert.False(index.IsUnique);
        Assert.Equal(["user_id"], index.Columns);
    }

    private sealed class TestMigration
        : ExternalIdentityMappings
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
