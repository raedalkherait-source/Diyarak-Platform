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
