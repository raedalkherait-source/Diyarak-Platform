using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingAvailableFromDateTests
{
    [Fact]
    public void Constructor_sets_value()
    {
        var date = new DateOnly(2026, 10, 15);

        var availableFrom = new ListingAvailableFromDate(date);

        Assert.Equal(date, availableFrom.Value);
    }

    [Fact]
    public void Equal_dates_are_equal()
    {
        var first = new ListingAvailableFromDate(new DateOnly(2026, 10, 15));
        var second = new ListingAvailableFromDate(new DateOnly(2026, 10, 15));

        Assert.Equal(first, second);
    }
}
