using Diyarak.Platform.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlExternalIdentityResolverTests
{
    [Fact]
    public async Task ResolveUserIdAsync_returns_exact_mapping()
    {
        await using PlatformDbContext context = CreateContext();
        Guid userId = Guid.NewGuid();

        context.ExternalIdentityMappings.Add(
            new ExternalIdentityMappingRecord
            {
                Issuer = "https://idp.example.test/",
                Subject = "Case-Sensitive-Subject",
                UserId = userId,
            });

        await context.SaveChangesAsync();

        var resolver =
            new PostgreSqlExternalIdentityResolver(context);

        Guid? resolved =
            await resolver.ResolveUserIdAsync(
                new ExternalIdentity(
                    "https://idp.example.test/",
                    "Case-Sensitive-Subject"));

        Assert.Equal(userId, resolved);
    }

    [Fact]
    public async Task ResolveUserIdAsync_does_not_normalize_subject()
    {
        await using PlatformDbContext context = CreateContext();

        context.ExternalIdentityMappings.Add(
            new ExternalIdentityMappingRecord
            {
                Issuer = "https://idp.example.test/",
                Subject = "Subject-A",
                UserId = Guid.NewGuid(),
            });

        await context.SaveChangesAsync();

        var resolver =
            new PostgreSqlExternalIdentityResolver(context);

        Guid? resolved =
            await resolver.ResolveUserIdAsync(
                new ExternalIdentity(
                    "https://idp.example.test/",
                    "subject-a"));

        Assert.Null(resolved);
    }

    [Fact]
    public async Task ResolveUserIdAsync_returns_null_when_mapping_is_missing()
    {
        await using PlatformDbContext context = CreateContext();

        var resolver =
            new PostgreSqlExternalIdentityResolver(context);

        Guid? resolved =
            await resolver.ResolveUserIdAsync(
                new ExternalIdentity(
                    "https://idp.example.test/",
                    "unknown-subject"));

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Multiple_external_identities_can_map_to_same_user()
    {
        await using PlatformDbContext context = CreateContext();
        Guid userId = Guid.NewGuid();

        context.ExternalIdentityMappings.AddRange(
            new ExternalIdentityMappingRecord
            {
                Issuer = "https://idp-a.example.test/",
                Subject = "subject-a",
                UserId = userId,
            },
            new ExternalIdentityMappingRecord
            {
                Issuer = "https://idp-b.example.test/",
                Subject = "subject-b",
                UserId = userId,
            });

        await context.SaveChangesAsync();

        var resolver =
            new PostgreSqlExternalIdentityResolver(context);

        Assert.Equal(
            userId,
            await resolver.ResolveUserIdAsync(
                new ExternalIdentity(
                    "https://idp-a.example.test/",
                    "subject-a")));

        Assert.Equal(
            userId,
            await resolver.ResolveUserIdAsync(
                new ExternalIdentity(
                    "https://idp-b.example.test/",
                    "subject-b")));
    }

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new PlatformDbContext(options);
    }
}
