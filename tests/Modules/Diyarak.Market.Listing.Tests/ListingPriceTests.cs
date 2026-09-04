using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingPriceTests
{
    [Fact]
    public void Known_price_exposes_amount()
    {
        var amount = new Money(250000m, Currency.Eur);

        var price = ListingPrice.Known(amount);

        Assert.Equal(amount, price.Amount);
        Assert.False(price.IsOnRequest);
    }

    [Fact]
    public void On_request_price_has_no_amount()
    {
        var price = ListingPrice.OnRequest();

        Assert.Null(price.Amount);
        Assert.True(price.IsOnRequest);
    }

    [Fact]
    public void Known_price_rejects_negative_amount()
    {
        var amount = new Money(-1m, Currency.Eur);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ListingPrice.Known(amount));
    }

    [Fact]
    public void Equal_known_prices_are_equal()
    {
        var first = ListingPrice.Known(new Money(1000m, Currency.Eur));
        var second = ListingPrice.Known(new Money(1000m, Currency.Eur));

        Assert.Equal(first, second);
    }
}
