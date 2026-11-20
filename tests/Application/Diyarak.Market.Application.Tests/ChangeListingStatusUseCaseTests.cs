using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ChangeListingStatusUseCaseTests
{
    [Fact]
    public async Task Owner_can_pause_resume_and_close_with_version_increments()
    {
        MarketListing listing = CreatePublished();
        var repository = new Repository(listing);
        var useCase = new ChangeListingStatusUseCase(repository, new StubMarketTransactionRunner());

        Result<long> paused = await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Pause);
        Assert.True(paused.IsSuccess);
        Assert.Equal(listing.Version + 1, paused.Value);
        Assert.Equal(ListingStatus.Paused, listing.Status);
        Assert.Equal(listing.Version, repository.LastExpectedVersion);

        Assert.True((await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Resume)).IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);

        Assert.True((await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Close, ListingClosureReason.Rented)).IsSuccess);
        Assert.Equal(ListingStatus.Closed, listing.Status);
        Assert.Equal(ListingClosureReason.Rented, listing.ClosureReason);
    }

    [Fact]
    public async Task Non_owner_gets_concealed_not_found()
    {
        MarketListing listing = CreatePublished();
        var useCase = new ChangeListingStatusUseCase(new Repository(listing), new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(listing.Id, Guid.NewGuid(), ListingStatusChange.Pause);

        Assert.Equal(ChangeListingStatusErrors.NotFound, result.Error);
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    [Fact]
    public async Task Invalid_transition_and_invalid_requests_are_rejected()
    {
        MarketListing listing = CreatePublished();
        var repository = new Repository(listing);
        var useCase = new ChangeListingStatusUseCase(repository, new StubMarketTransactionRunner());

        Assert.Equal(
            ChangeListingStatusErrors.InvalidTransition,
            (await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Resume)).Error);
        Assert.Equal(
            ChangeListingStatusErrors.InvalidRequest,
            (await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Close)).Error);
        Assert.Equal(
            ChangeListingStatusErrors.InvalidRequest,
            (await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Pause, ListingClosureReason.Sold)).Error);
        Assert.Equal(
            ChangeListingStatusErrors.InvalidRequest,
            (await useCase.ExecuteAsync(Guid.Empty, listing.PublisherUserId, ListingStatusChange.Pause)).Error);
        Assert.Null(repository.LastExpectedVersion);
    }

    [Fact]
    public async Task Concurrent_save_failure_is_reported()
    {
        MarketListing listing = CreatePublished();
        var useCase = new ChangeListingStatusUseCase(new Repository(listing, saveAccepted: false), new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, ListingStatusChange.Pause);

        Assert.Equal(ChangeListingStatusErrors.ConcurrentModification, result.Error);
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

    private sealed class Repository(MarketListing listing, bool saveAccepted = true) : IMarketListingRepository
    {
        public long? LastExpectedVersion { get; private set; }

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(listing.Id == listingId ? listing : null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([listing]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default)
        {
            LastExpectedVersion = expectedVersion;
            return Task.FromResult(saveAccepted);
        }
    }
}
