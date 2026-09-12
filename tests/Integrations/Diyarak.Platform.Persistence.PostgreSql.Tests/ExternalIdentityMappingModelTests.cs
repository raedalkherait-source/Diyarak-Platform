using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class ExternalIdentityMappingModelTests
{
    [Fact]
    public void Model_contains_external_identity_mapping_constraints()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_external_identity_model_test")
                .Options;

        using var context = new PlatformDbContext(options);

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;

        var entityType = Assert.Single(
            designTimeModel.GetEntityTypes(),
            candidate =>
                candidate.ClrType.Name ==
                "ExternalIdentityMappingRecord");

        Assert.Equal("identity", entityType.GetSchema());
        Assert.Equal(
            "external_identity_mappings",
            entityType.GetTableName());

        Assert.Equal(
            ["Issuer", "Subject"],
            entityType.FindPrimaryKey()!
                .Properties
                .Select(property => property.Name));

        var userIdIndex = Assert.Single(
            entityType.GetIndexes(),
            index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == "UserId");

        Assert.False(userIdIndex.IsUnique);

        var checkConstraint = Assert.Single(
            entityType.GetCheckConstraints(),
            constraint =>
                constraint.Name ==
                "ck_external_identity_mappings_user_id_non_empty");

        Assert.Contains(
            "00000000-0000-0000-0000-000000000000",
            checkConstraint.Sql);
    }
}
