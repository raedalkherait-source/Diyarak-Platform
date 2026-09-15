using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class GetPublishedListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_listing_identifier()
    {
        var repository = new StubMarketListingRepository(null);
        var useCase = new GetPublishedListingUseCase(repository);

        Result<MarketListing> result = await useCase.ExecuteAsync(
            Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.InvalidIdentifier, result.Error);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_listing_is_missing()
    {
        var repository = new StubMarketListingRepository(null);
        var useCase = new GetPublishedListingUseCase(repository);

        Result<MarketListing> result = await useCase.ExecuteAsync(
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.NotFound, result.Error);
        Assert.Equal(1, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_for_draft_listing()
    {
        MarketListing listing = CreateReadyListing();
        var repository = new StubMarketListingRepository(listing);
        var useCase = new GetPublishedListingUseCase(repository);

        Result<MarketListing> result = await useCase.ExecuteAsync(
            listing.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.NotFound, result.Error);
        Assert.Equal(1, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_published_listing_without_actor_requirement()
    {
        MarketListing listing = CreateReadyListing();
        listing.Publish();
        var repository = new StubMarketListingRepository(listing);
        var useCase = new GetPublishedListingUseCase(repository);

        Result<MarketListing> result = await useCase.ExecuteAsync(
            listing.Id);

        Assert.True(result.IsSuccess);
        Assert.Same(listing, result.Value);
        Assert.Equal(1, repository.FindCallCount);
    }

    private static MarketListing CreateReadyListing()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

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
        MarketListing? listing)
        : IMarketListingRepository
    {
        public int FindCallCount { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;

            return Task.FromResult(
                listing?.Id == listingId
                    ? listing
                    : null);
        }

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(
            Guid publisherUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>(
                Array.Empty<MarketListing>());

        public Task AddAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(
            MarketListing listing,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
