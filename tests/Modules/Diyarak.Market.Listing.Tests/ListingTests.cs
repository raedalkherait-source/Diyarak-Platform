using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingTests
{
    [Fact]
    public void Constructor_sets_identity_and_subject_reference()
    {
        var id = Guid.NewGuid();
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(id, subjectReference);

        Assert.Equal(id, listing.Id);
        Assert.Equal(subjectReference, listing.SubjectReference);
    }

    [Fact]
    public void Constructor_starts_listing_as_draft()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);

        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Fact]
    public void Publish_sets_status_to_published()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);

        listing.Publish();

        Assert.Equal(ListingStatus.Published, listing.Status);
    }
    [Fact]
    public void Publish_rejects_listing_that_is_already_published()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.Publish();

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }
    [Fact]
    public void Constructor_rejects_null_subject_reference()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MarketListing(Guid.NewGuid(), null!));
    }

    [Fact]
    public void Constructor_rejects_unsupported_subject_type()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            "market.unsupported");

        Assert.Throws<ArgumentException>(
            () => new MarketListing(Guid.NewGuid(), subjectReference));
    }
}
