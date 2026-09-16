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

        Result<OwnedListingPage> result =
            await useCase.ExecuteAsync(Guid.Empty, 1, 20);

        Assert.True(result.IsFailure);
        Assert.Equal(ListOwnedListingsErrors.InvalidActorIdentifier, result.Error);
        Assert.Equal(0, repository.FindByPublisherCallCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ExecuteAsync_returns_validation_failure_for_invalid_pagination(
        int page,
        int pageSize)
    {
        var repository = new StubMarketListingRepository([]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<OwnedListingPage> result =
            await useCase.ExecuteAsync(Guid.NewGuid(), page, pageSize);

        Assert.True(result.IsFailure);
        Assert.Equal(ListOwnedListingsErrors.InvalidPagination, result.Error);
        Assert.Equal(0, repository.FindByPublisherCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_bounded_ordered_page_and_has_more()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketListing third = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            actorUserId);
        MarketListing first = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            actorUserId);
        MarketListing second = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            actorUserId);
        var repository = new StubMarketListingRepository(
            [third, first, second]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<OwnedListingPage> result =
            await useCase.ExecuteAsync(actorUserId, 1, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.True(result.Value.HasMore);
        Assert.Equal(new[] { first.Id, second.Id }, result.Value.Items.Select(item => item.Id));
        Assert.Equal(1, repository.FindByPublisherCallCount);
        Assert.Equal(actorUserId, repository.LastPublisherUserId);
    }

    [Fact]
    public async Task ExecuteAsync_returns_second_page_without_has_more()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketListing first = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), actorUserId);
        MarketListing second = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"), actorUserId);
        MarketListing third = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"), actorUserId);
        var repository = new StubMarketListingRepository([first, second, third]);
        var useCase = new ListOwnedListingsUseCase(repository);

        Result<OwnedListingPage> result =
            await useCase.ExecuteAsync(actorUserId, 2, 2);

        Assert.True(result.IsSuccess);
        MarketListing item = Assert.Single(result.Value.Items);
        Assert.Equal(third.Id, item.Id);
        Assert.False(result.Value.HasMore);
    }

    [Fact]
    public async Task ExecuteAsync_fails_closed_when_repository_returns_other_owner()
    {
        Guid actorUserId = Guid.NewGuid();
        var repository = new StubMarketListingRepository(
            [CreateListing(Guid.NewGuid(), Guid.NewGuid())]);
        var useCase = new ListOwnedListingsUseCase(repository);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.ExecuteAsync(actorUserId, 1, 20));

        Assert.Contains("another publisher", exception.Message);
    }

    private static MarketListing CreateListing(
        Guid id,
        Guid publisherUserId) =>
        new(
            id,
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
