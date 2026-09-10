using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlPropertyExistenceCheckerTests
{
    [Fact]
    public async Task ExistsAsync_returns_true_when_property_exists()
    {
        Guid propertyId = Guid.NewGuid();

        await using PlatformDbContext context = CreateContext();

        context.MarketProperties.Add(CreateRecord(propertyId));
        await context.SaveChangesAsync();

        var checker =
            new PostgreSqlPropertyExistenceChecker(context);

        bool exists = await checker.ExistsAsync(propertyId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_returns_false_when_property_does_not_exist()
    {
        await using PlatformDbContext context = CreateContext();

        var checker =
            new PostgreSqlPropertyExistenceChecker(context);

        bool exists = await checker.ExistsAsync(Guid.NewGuid());

        Assert.False(exists);
    }

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }

    private static MarketPropertyRecord CreateRecord(
        Guid propertyId)
    {
        return new MarketPropertyRecord
        {
            Id = propertyId,
            Category = 1,
            Street = "Market Street",
            HouseNumber = "42",
            PostalCode = "10115",
            City = "Berlin",
            Features = [],
        };
    }
}
