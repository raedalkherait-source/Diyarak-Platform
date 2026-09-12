using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingRecordMapperTests
{
    [Fact]
    public void Round_trip_preserves_draft_with_unset_optional_state()
    {
        MarketListing original = CreateListing();

        var record =
            MarketListingRecordMapper.FromDomain(original);

        MarketListing restored =
            MarketListingRecordMapper.ToDomain(record);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(
            original.PublisherUserId,
            record.PublisherUserId);
        Assert.Equal(
            original.PublisherUserId,
            restored.PublisherUserId);
        Assert.Equal(
            original.SubjectReference,
            restored.SubjectReference);
        Assert.Equal(ListingStatus.Draft, restored.Status);
        Assert.Equal(original.Version, record.Version);
        Assert.Equal(original.Version, restored.Version);
        Assert.Null(restored.Context);
        Assert.Null(restored.Headline);
        Assert.Null(restored.Price);
        Assert.Null(restored.AvailableFromDate);
        Assert.Null(record.PriceIsOnRequest);
        Assert.Null(record.PriceAmount);
        Assert.Null(record.PriceCurrency);
    }

    [Fact]
    public void Round_trip_preserves_published_on_request_listing()
    {
        MarketListing original = CreateListing();

        original.SetContext(
            new ListingContext(
                PublishingRole.Tenant,
                TransactionIntent.Rent));

        original.SetHeadline(
            new ListingHeadline("Apartment for rent"));

        original.SetPrice(ListingPrice.OnRequest());

        original.SetAvailableFromDate(
            new ListingAvailableFromDate(
                new DateOnly(2027, 1, 15)));

        original.Publish();

        var record =
            MarketListingRecordMapper.FromDomain(original);

        MarketListing restored =
            MarketListingRecordMapper.ToDomain(record);

        Assert.Equal(ListingStatus.Published, restored.Status);
        Assert.Equal(original.Context, restored.Context);
        Assert.Equal(original.Headline, restored.Headline);
        Assert.Equal(original.Price, restored.Price);
        Assert.Equal(
            original.AvailableFromDate,
            restored.AvailableFromDate);
        Assert.Equal(true, record.PriceIsOnRequest);
        Assert.Null(record.PriceAmount);
        Assert.Null(record.PriceCurrency);
    }

    [Fact]
    public void Round_trip_preserves_published_known_price_listing()
    {
        MarketListing original = CreateListing();

        original.SetContext(
            new ListingContext(
                PublishingRole.ProfessionalOrAgent,
                TransactionIntent.Sell));

        original.SetHeadline(
            new ListingHeadline("Commercial property for sale"));

        original.SetPrice(
            ListingPrice.Known(
                new Money(250_000m, Currency.Eur)));

        original.Publish();

        var record =
            MarketListingRecordMapper.FromDomain(original);

        MarketListing restored =
            MarketListingRecordMapper.ToDomain(record);

        Assert.Equal(ListingStatus.Published, restored.Status);
        Assert.Equal(original.Context, restored.Context);
        Assert.Equal(original.Headline, restored.Headline);
        Assert.Equal(original.Price, restored.Price);
        Assert.Equal(false, record.PriceIsOnRequest);
        Assert.Equal(250_000m, record.PriceAmount);
        Assert.Equal("EUR", record.PriceCurrency);
    }

    private static MarketListing CreateListing()
    {
        return new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));
    }
}
