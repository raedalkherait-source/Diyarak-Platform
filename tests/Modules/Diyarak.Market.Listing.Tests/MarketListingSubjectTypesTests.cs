using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class MarketListingSubjectTypesTests
{
    [Fact]
    public void Property_has_stable_sector_qualified_value()
    {
        Assert.Equal("market.property", MarketListingSubjectTypes.Property);
    }
}
