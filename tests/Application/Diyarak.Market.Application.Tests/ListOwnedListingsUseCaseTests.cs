using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListOwnedListingsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository = new StubMarketListingRepository([]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<IReadOnlyList<MarketListing>> result =
            await useCase.ExecuteAsync(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ListOwnedListingsErrors.InvalidActorIdentifier,
            result.Error);
        Assert.Equal(0, repository.FindByPublisherCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_collection_when_actor_has_no_listings()
    {
        var repository = new StubMarketListingRepository([]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<IReadOnlyList<MarketListing>> result =
            await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, repository.FindByPublisherCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_repository_listings_for_actor()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketListing first = CreateListing(actorUserId);
        MarketListing second = CreateListing(actorUserId);
        var repository = new StubMarketListingRepository(
            [first, second]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<IReadOnlyList<MarketListing>> result =
            await useCase.ExecuteAsync(actorUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(first, result.Value);
        Assert.Contains(second, result.Value);
        Assert.Equal(1, repository.FindByPublisherCallCount);
        Assert.Equal(actorUserId, repository.LastPublisherUserId);
    }

    private static MarketListing CreateListing(Guid publisherUserId) =>
        new(
            Guid.NewGuid(),
            publisherUserId,
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

    private sealed class StubMarketListingRepository(
        IReadOnlyList<MarketListing> listings)
        : IMarketListingRepository
    {
        public int FindByPublisherCallCount { get; private set; }

        public Guid? LastPublisherUserId { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketListing?>(null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(
            Guid publisherUserId,
            CancellationToken cancellationToken = default)
        {
            FindByPublisherCallCount++;
            LastPublisherUserId = publisherUserId;
            return Task.FromResult(listings);
        }

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
