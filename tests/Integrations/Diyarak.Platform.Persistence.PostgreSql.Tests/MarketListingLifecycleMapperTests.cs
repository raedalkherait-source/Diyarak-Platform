using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingLifecycleMapperTests
{
    [Fact]
    public void Paused_listing_round_trips()
    {
        MarketListing listing = CreatePublished();
        listing.Pause();

        MarketListing restored = MarketListingRecordMapper.ToDomain(MarketListingRecordMapper.FromDomain(listing));

        Assert.Equal(ListingStatus.Paused, restored.Status);
        Assert.Null(restored.ClosureReason);
    }

    [Fact]
    public void Closed_listing_round_trips_with_reason()
    {
        MarketListing listing = CreatePublished();
        listing.Close(ListingClosureReason.Sold);

        MarketListingRecord record = MarketListingRecordMapper.FromDomain(listing);
        MarketListing restored = MarketListingRecordMapper.ToDomain(record);

        Assert.Equal((int)ListingClosureReason.Sold, record.ClosureReason);
        Assert.Equal(ListingStatus.Closed, restored.Status);
        Assert.Equal(ListingClosureReason.Sold, restored.ClosureReason);
    }

    [Fact]
    public void Inconsistent_closure_data_is_rejected()
    {
        MarketListing closed = CreatePublished();
        closed.Close(ListingClosureReason.Rented);
        MarketListingRecord missingReason = MarketListingRecordMapper.FromDomain(closed);
        missingReason.ClosureReason = null;

        MarketListingRecord unexpectedReason = MarketListingRecordMapper.FromDomain(CreatePublished());
        unexpectedReason.ClosureReason = (int)ListingClosureReason.Sold;

        Assert.Throws<InvalidOperationException>(() => MarketListingRecordMapper.ToDomain(missingReason));
        Assert.Throws<InvalidOperationException>(() => MarketListingRecordMapper.ToDomain(unexpectedReason));
    }

    private static MarketListing CreatePublished()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();
        return listing;
    }
}
