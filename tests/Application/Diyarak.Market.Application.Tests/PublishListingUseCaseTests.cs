using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class PublishListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_rejects_when_listing_does_not_exist()
    {
        var repository =
            new StubMarketListingRepository(storedListing: null);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var useCase =
            new PublishListingUseCase(repository, checker);

        Guid listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(listingId));

        Assert.Equal(listingId, repository.LastRequestedId);
        Assert.Null(repository.SavedListing);
        Assert.Equal(0, checker.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_when_property_does_not_exist()
    {
        MarketListing listing = CreateReadyListing();

        var repository =
            new StubMarketListingRepository(listing);

        var checker =
            new StubPropertyExistenceChecker(exists: false);

        var useCase =
            new PublishListingUseCase(repository, checker);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(listing.Id));

        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(
            listing.SubjectReference.SubjectId,
            checker.LastPropertyId);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_and_saves_when_property_exists()
    {
        MarketListing listing = CreateReadyListing();

        var repository =
            new StubMarketListingRepository(listing);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var useCase =
            new PublishListingUseCase(repository, checker);

        await useCase.ExecuteAsync(listing.Id);

        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(
            listing.SubjectReference.SubjectId,
            checker.LastPropertyId);
        Assert.Same(listing, repository.SavedListing);
    }

    private static MarketListing CreateReadyListing()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        var listing =
            new MarketListing(Guid.NewGuid(), subjectReference);

        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));

        listing.SetHeadline(
            new ListingHeadline("Property for sale"));

        listing.SetPrice(ListingPrice.OnRequest());

        return listing;
    }

    private sealed class StubMarketListingRepository(
        MarketListing? storedListing)
        : IMarketListingRepository
    {
        public Guid? LastRequestedId { get; private set; }

        public MarketListing? SavedListing { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            LastRequestedId = listingId;

            MarketListing? result =
                storedListing?.Id == listingId
                    ? storedListing
                    : null;

            return Task.FromResult(result);
        }

        public Task SaveAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default)
        {
            SavedListing = listing;

            return Task.CompletedTask;
        }
    }

    private sealed class StubPropertyExistenceChecker(bool exists)
        : IPropertyExistenceChecker
    {
        public int CallCount { get; private set; }

        public Guid? LastPropertyId { get; private set; }

        public Task<bool> ExistsAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPropertyId = propertyId;

            return Task.FromResult(exists);
        }
    }
}
