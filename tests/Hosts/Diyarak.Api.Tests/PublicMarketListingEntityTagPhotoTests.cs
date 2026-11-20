using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Tests;

public sealed class PublicMarketListingEntityTagPhotoTests
{
    [Fact]
    public void Detail_and_collection_validators_change_when_photos_change()
    {
        MarketListing listing = CreatePublished();
        var property = new MarketProperty(
            listing.SubjectReference.SubjectId,
            Diyarak.Market.Property.PropertyCategory.Apartment,
            new Diyarak.Market.Property.PropertyAddress(null, null, null, "Damascus"));

        var withoutPhotos = new PublishedListingProjection(listing, property);
        var withPhoto = withoutPhotos with { Photos = [Photo(listing.Id, 0)] };
        var withOtherPhoto = withoutPhotos with { Photos = [Photo(listing.Id, 0)] };

        Assert.NotEqual(PublicMarketListingEntityTag.Create(withoutPhotos), PublicMarketListingEntityTag.Create(withPhoto));
        Assert.NotEqual(PublicMarketListingEntityTag.Create(withPhoto), PublicMarketListingEntityTag.Create(withOtherPhoto));
        Assert.Equal(PublicMarketListingEntityTag.Create(withPhoto), PublicMarketListingEntityTag.Create(withPhoto with { }));

        Assert.NotEqual(
            PublicMarketListingEntityTag.Create(new PublishedListingPage([withoutPhotos], 1, 20, false)),
            PublicMarketListingEntityTag.Create(new PublishedListingPage([withPhoto], 1, 20, false)));
    }

    private static ListingPhoto Photo(Guid listingId, int position)
    {
        Guid id = Guid.NewGuid();
        return new ListingPhoto(id, listingId, ListingPhoto.CreateStorageKey(listingId, id, ListingPhotoFormat.Jpeg), ListingPhotoFormat.Jpeg, 10, position, DateTimeOffset.UnixEpoch);
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
