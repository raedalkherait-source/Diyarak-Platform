using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketPropertyVersionModelTests
{
    [Fact]
    public void Property_version_is_required_bigint_concurrency_token()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql("Host=localhost;Database=diyarak_model_test")
                .Options;

        using var context = new PlatformDbContext(options);

        var entityType = context.Model.FindEntityType(
            typeof(MarketPropertyRecord));
        Assert.NotNull(entityType);

        var version = entityType!.FindProperty("Version");
        Assert.NotNull(version);
        Assert.False(version!.IsNullable);
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal("version", version.GetColumnName());
        Assert.Equal("bigint", version.GetColumnType());
    }
}
