using Diyarak.Platform.Listing;
using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingLifecycleTests
{
    [Fact]
    public void Published_listing_can_be_paused_resumed_and_closed()
    {
        Listing listing = CreatePublished();

        listing.Pause();
        Assert.Equal(ListingStatus.Paused, listing.Status);
        Assert.False(listing.IsPubliclyVisible);
        Assert.True(listing.IsEditable);

        listing.SetPrice(ListingPrice.OnRequest());

        listing.Resume();
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.True(listing.IsPubliclyVisible);

        listing.Close(ListingClosureReason.Sold);
        Assert.Equal(ListingStatus.Closed, listing.Status);
        Assert.Equal(ListingClosureReason.Sold, listing.ClosureReason);
        Assert.False(listing.IsEditable);
        Assert.False(listing.IsPubliclyVisible);
    }

    [Fact]
    public void Paused_listing_can_be_closed_directly()
    {
        Listing listing = CreatePublished();
        listing.Pause();

        listing.Close(ListingClosureReason.Withdrawn);

        Assert.Equal(ListingStatus.Closed, listing.Status);
    }

    [Fact]
    public void Invalid_transitions_are_rejected()
    {
        Listing draft = CreateDraft();
        Assert.Throws<InvalidOperationException>(draft.Pause);
        Assert.Throws<InvalidOperationException>(draft.Resume);
        Assert.Throws<InvalidOperationException>(() => draft.Close(ListingClosureReason.Withdrawn));

        Listing published = CreatePublished();
        Assert.Throws<InvalidOperationException>(published.Resume);
        Assert.Throws<InvalidOperationException>(published.Publish);

        published.Close(ListingClosureReason.Rented);
        Assert.Throws<InvalidOperationException>(published.Pause);
        Assert.Throws<InvalidOperationException>(published.Resume);
        Assert.Throws<InvalidOperationException>(() => published.Close(ListingClosureReason.Sold));
        Assert.Equal(ListingClosureReason.Rented, published.ClosureReason);
    }

    [Fact]
    public void Close_rejects_undefined_reason()
    {
        Listing listing = CreatePublished();

        Assert.Throws<ArgumentOutOfRangeException>(() => listing.Close((ListingClosureReason)42));
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    private static Listing CreateDraft()
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

    private static Listing CreatePublished()
    {
        Listing listing = CreateDraft();
        listing.Publish();
        return listing;
    }
}
