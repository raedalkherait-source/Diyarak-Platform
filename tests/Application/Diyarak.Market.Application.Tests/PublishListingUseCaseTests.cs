using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class PublishListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_rejects_listing_when_property_does_not_exist()
    {
        var checker = new StubPropertyExistenceChecker(exists: false);
        var useCase = new PublishListingUseCase(checker);

        var listing = CreateReadyListing();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(listing));

        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_listing_when_property_exists()
    {
        var checker = new StubPropertyExistenceChecker(exists: true);
        var useCase = new PublishListingUseCase(checker);

        var listing = CreateReadyListing();

        await useCase.ExecuteAsync(listing);

        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(listing.SubjectReference.SubjectId, checker.LastPropertyId);
    }
    private static MarketListing CreateReadyListing()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing = new MarketListing(Guid.NewGuid(), subjectReference);
        listing.SetContext(
            new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());

        return listing;
    }

    private sealed class StubPropertyExistenceChecker(bool exists)
        : IPropertyExistenceChecker
    {
        public Guid? LastPropertyId { get; private set; }

        public Task<bool> ExistsAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            LastPropertyId = propertyId;

            return Task.FromResult(exists);
        }
    }
}
