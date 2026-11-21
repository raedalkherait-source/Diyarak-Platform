using Diyarak.Api.Market;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class SavedSearchQueryTests
{
    [Fact]
    public void Canonical_form_drops_paging_and_unknown_keys_and_sorts()
    {
        string? a = MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize(
            "page=3&pageSize=12&governorate=Damascus&transactionIntent=Sell&utm_source=x&propertyCategory=Apartment&propertyCategory=House");
        string? b = MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize(
            "propertyCategory=House&transactionIntent=Sell&propertyCategory=Apartment&governorate=Damascus");

        Assert.Equal("governorate=Damascus&propertyCategory=Apartment&propertyCategory=House&transactionIntent=Sell", a);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Arabic_values_round_trip_and_parse()
    {
        string? canonical = MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize("district=%D8%A7%D9%84%D9%85%D8%B2%D8%A9&governorate=Damascus");

        Assert.NotNull(canonical);
        Assert.Equal("المزة", MarketSavedSearchEndpointRouteBuilderExtensions.Parse(canonical!)!.Property!.Location!.District);
    }

    [Theory]
    [InlineData("transactionIntent=Steal")]
    [InlineData("priceMin=abc&priceCurrency=USD")]
    public void Invalid_searches_are_rejected(string query)
    {
        Assert.Null(MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize(query));
    }

    [Fact]
    public void An_unfiltered_search_is_valid()
    {
        Assert.Equal(string.Empty, MarketSavedSearchEndpointRouteBuilderExtensions.Canonicalize("page=1"));
        Assert.NotNull(MarketSavedSearchEndpointRouteBuilderExtensions.Parse(string.Empty));
    }
}
