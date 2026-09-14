using Diyarak.Market.Property;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlPropertyListingAuthorizationCheckerTests
{
    [Fact]
    public async Task CanCreateListingAsync_returns_true_for_matching_owner()
    {
        await using PlatformDbContext context = CreateContext();
        Guid ownerUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(ownerUserId);
        var repository = new PostgreSqlMarketPropertyRepository(context);
        await repository.AddAsync(property);
        context.ChangeTracker.Clear();
        var checker =
            new PostgreSqlPropertyListingAuthorizationChecker(context);

        bool result = await checker.CanCreateListingAsync(
            property.Id,
            ownerUserId);

        Assert.True(result);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CanCreateListingAsync_returns_false_for_different_owner()
    {
        await using PlatformDbContext context = CreateContext();
        MarketProperty property = CreateProperty(Guid.NewGuid());
        var repository = new PostgreSqlMarketPropertyRepository(context);
        await repository.AddAsync(property);
        context.ChangeTracker.Clear();
        var checker =
            new PostgreSqlPropertyListingAuthorizationChecker(context);

        bool result = await checker.CanCreateListingAsync(
            property.Id,
            Guid.NewGuid());

        Assert.False(result);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CanCreateListingAsync_returns_false_for_legacy_unowned_property()
    {
        await using PlatformDbContext context = CreateContext();
        MarketProperty property = CreateProperty(ownerUserId: null);
        var repository = new PostgreSqlMarketPropertyRepository(context);
        await repository.AddAsync(property);
        context.ChangeTracker.Clear();
        var checker =
            new PostgreSqlPropertyListingAuthorizationChecker(context);

        bool result = await checker.CanCreateListingAsync(
            property.Id,
            Guid.NewGuid());

        Assert.False(result);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CanCreateListingAsync_returns_false_for_missing_property()
    {
        await using PlatformDbContext context = CreateContext();
        var checker =
            new PostgreSqlPropertyListingAuthorizationChecker(context);

        bool result = await checker.CanCreateListingAsync(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.False(result);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CanCreateListingAsync_returns_false_for_empty_identifiers(
        bool emptyPropertyId,
        bool emptyActorUserId)
    {
        await using PlatformDbContext context = CreateContext();
        var checker =
            new PostgreSqlPropertyListingAuthorizationChecker(context);

        bool result = await checker.CanCreateListingAsync(
            emptyPropertyId ? Guid.Empty : Guid.NewGuid(),
            emptyActorUserId ? Guid.Empty : Guid.NewGuid());

        Assert.False(result);
    }

    private static MarketProperty CreateProperty(Guid? ownerUserId) =>
        new(
            Guid.NewGuid(),
            PropertyCategory.House,
            new PropertyAddress(
                "Owner Street",
                "1",
                "23552",
                "Luebeck"),
            ownerUserId: ownerUserId);

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }
}
