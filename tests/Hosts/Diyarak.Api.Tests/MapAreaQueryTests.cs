using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class MapAreaQueryTests
{
    [Fact]
    public void Valid_bbox_parses_in_south_west_north_east_order()
    {
        Assert.True(PublicMarketListingEndpointRouteBuilderExtensions.TryParseMapArea("33.45,36.2,33.55,36.35", out PublishedMapArea? area));
        Assert.Equal(new PublishedMapArea(33.45, 36.2, 33.55, 36.35), area);
    }

    [Theory]
    [InlineData("")]
    [InlineData("33.45,36.2,33.55")]
    [InlineData("33.55,36.2,33.45,36.35")]
    [InlineData("33.45,36.35,33.55,36.2")]
    [InlineData("20,20,40,40")]
    [InlineData("NaN,36,34,37")]
    [InlineData("1e1,36,34,37")]
    [InlineData("33.45, 36.2,33.55,36.35")]
    public void Invalid_bbox_is_rejected(string value)
    {
        Assert.False(PublicMarketListingEndpointRouteBuilderExtensions.TryParseMapArea(value, out _));
    }

    [Fact]
    public void Saved_searches_keep_the_map_area()
    {
        Assert.Equal(
            "bbox=33.45%2C36.2%2C33.55%2C36.35&governorate=Damascus",
            MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize("governorate=Damascus&bbox=33.45,36.2,33.55,36.35"));
    }

    [Fact]
    public void A_bbox_alone_is_a_search_criterion()
    {
        var query = new Microsoft.AspNetCore.Http.QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["bbox"] = "33.45,36.2,33.55,36.35" });

        Assert.True(PublicMarketListingEndpointRouteBuilderExtensions.TryParseSearchCriteria(query, out PublishedListingSearchCriteria? criteria));
        Assert.Equal(new PublishedMapArea(33.45, 36.2, 33.55, 36.35), criteria!.MapArea);

        var invalid = new Microsoft.AspNetCore.Http.QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["bbox"] = "1,2,3" });
        Assert.False(PublicMarketListingEndpointRouteBuilderExtensions.TryParseSearchCriteria(invalid, out _));
    }
}
