using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingHeadlineTests
{
    [Fact]
    public void Constructor_sets_value()
    {
        var headline = new ListingHeadline("Modern commercial space");

        Assert.Equal("Modern commercial space", headline.Value);
    }

    [Fact]
    public void Equal_headlines_are_equal()
    {
        var first = new ListingHeadline("Modern commercial space");
        var second = new ListingHeadline("Modern commercial space");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_or_whitespace_value(string value)
    {
        Assert.Throws<ArgumentException>(
            () => new ListingHeadline(value));
    }
}
