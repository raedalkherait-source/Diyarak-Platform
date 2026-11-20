using Diyarak.Platform.Listing;
using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingContactOptionsTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Valid_channel_combinations_are_accepted(bool calls, bool whatsApp)
    {
        var options = new ListingContactOptions(calls, whatsApp);

        Assert.Equal(calls, options.AllowCalls);
        Assert.Equal(whatsApp, options.AllowWhatsApp);
        Assert.Equal(options, new ListingContactOptions(calls, whatsApp));
    }

    [Fact]
    public void At_least_one_channel_is_required()
    {
        Assert.Throws<ArgumentException>(() => new ListingContactOptions(false, false));
    }

    [Fact]
    public void Contact_can_be_changed_until_listing_is_closed()
    {
        Listing listing = CreateReadyListing();

        listing.SetContact(new ListingContactOptions(true, true));
        Assert.Equal(new ListingContactOptions(true, true), listing.Contact);

        listing.ClearContact();
        Assert.Null(listing.Contact);

        listing.Publish();
        listing.SetContact(new ListingContactOptions(false, true));
        listing.Close(ListingClosureReason.Rented);

        Assert.Throws<InvalidOperationException>(() => listing.SetContact(new ListingContactOptions(true, false)));
        Assert.Throws<InvalidOperationException>(() => listing.ClearContact());
        Assert.Equal(new ListingContactOptions(false, true), listing.Contact);
    }

    private static Listing CreateReadyListing()
    {
        var listing = new Listing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        return listing;
    }
}
