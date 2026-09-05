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
    public void SetAvailableFromDate_assigns_date_while_draft()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        var availableFromDate =
            new ListingAvailableFromDate(new DateOnly(2026, 10, 1));

        listing.SetAvailableFromDate(availableFromDate);

        Assert.Equal(availableFromDate, listing.AvailableFromDate);
    }
    [Fact]
    public void Publish_rejects_draft_without_required_publication_data()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }
    [Fact]
    public void Publish_rejects_draft_without_context()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetHeadline(new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }

    [Fact]
    public void Publish_rejects_draft_without_headline()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetPrice(ListingPrice.OnRequest());

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }

    [Fact]
    public void Publish_rejects_draft_without_price()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Property for sale"));

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }
    [Fact]
    public void Publish_sets_status_to_published()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());

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
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();

        Assert.Throws<InvalidOperationException>(() => listing.Publish());
    }
    [Fact]
    public void Published_listing_rejects_core_publication_data_changes()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();

        Assert.Throws<InvalidOperationException>(
            () => listing.SetContext(
                new ListingContext(PublishingRole.Tenant, TransactionIntent.Rent)));
        Assert.Throws<InvalidOperationException>(
            () => listing.SetHeadline(new ListingHeadline("Updated headline")));
        Assert.Throws<InvalidOperationException>(
            () => listing.SetPrice(ListingPrice.OnRequest()));
        Assert.Throws<InvalidOperationException>(
            () => listing.SetAvailableFromDate(
                new ListingAvailableFromDate(new DateOnly(2026, 11, 1))));
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
