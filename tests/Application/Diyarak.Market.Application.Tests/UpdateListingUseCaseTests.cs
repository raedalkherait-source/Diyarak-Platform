using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class UpdateListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_listing_identifier()
    {
        var repository = new StubMarketListingRepository(null);
        var transactionRunner = new StubMarketTransactionRunner();
        var useCase = new UpdateListingUseCase(repository, transactionRunner);

        Result<long> result = await useCase.ExecuteAsync(
            Guid.Empty,
            Guid.NewGuid(),
            HeadlinePatch(1, "Updated"));

        Assert.True(result.IsFailure);
        Assert.Equal(UpdateListingErrors.InvalidIdentifier, result.Error);
        Assert.Equal(0, transactionRunner.CallCount);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_when_patch_has_no_changes()
    {
        var repository = new StubMarketListingRepository(null);
        var transactionRunner = new StubMarketTransactionRunner();
        var useCase = new UpdateListingUseCase(repository, transactionRunner);

        Result<long> result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateListingPatch(
                1,
                false,
                null,
                false,
                null,
                false,
                null,
                false,
                null));

        Assert.True(result.IsFailure);
        Assert.Equal(UpdateListingErrors.NoChanges, result.Error);
        Assert.Equal(0, transactionRunner.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_concealed_not_found_for_non_owner()
    {
        MarketListing listing = CreateDraftListing();
        var repository = new StubMarketListingRepository(listing);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            Guid.NewGuid(),
            HeadlinePatch(listing.Version, "Updated"));

        Assert.True(result.IsFailure);
        Assert.Equal(UpdateListingErrors.NotFound, result.Error);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_for_stale_version_before_mutation()
    {
        MarketListing listing = CreateDraftListing();
        listing.SetHeadline(new ListingHeadline("Original"));
        var repository = new StubMarketListingRepository(listing);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            listing.PublisherUserId,
            HeadlinePatch(listing.Version + 1, "Updated"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            UpdateListingErrors.ConcurrentModification,
            result.Error);
        Assert.Equal("Original", listing.Headline!.Value);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_when_listing_is_published()
    {
        MarketListing listing = CreateReadyListing();
        listing.Publish();
        var repository = new StubMarketListingRepository(listing);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            listing.PublisherUserId,
            HeadlinePatch(listing.Version, "Updated"));

        Assert.True(result.IsFailure);
        Assert.Equal(UpdateListingErrors.CannotEdit, result.Error);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_applies_partial_update_and_returns_next_version()
    {
        MarketListing listing = CreateDraftListing();
        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));
        var repository = new StubMarketListingRepository(listing);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            listing.PublisherUserId,
            HeadlinePatch(listing.Version, "Updated headline"));

        Assert.True(result.IsSuccess);
        Assert.Equal(listing.Version + 1, result.Value);
        Assert.Equal("Updated headline", listing.Headline!.Value);
        Assert.Equal(PublishingRole.Owner, listing.Context!.PublishingRole);
        Assert.Equal(listing.Version, repository.LastExpectedVersion);
        Assert.Same(listing, repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_can_clear_available_from_date()
    {
        MarketListing listing = CreateDraftListing();
        listing.SetAvailableFromDate(
            new ListingAvailableFromDate(new DateOnly(2026, 10, 1)));
        var repository = new StubMarketListingRepository(listing);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        var patch = new UpdateListingPatch(
            listing.Version,
            false,
            null,
            false,
            null,
            false,
            null,
            true,
            null);

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            listing.PublisherUserId,
            patch);

        Assert.True(result.IsSuccess);
        Assert.Null(listing.AvailableFromDate);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_when_save_detects_race()
    {
        MarketListing listing = CreateDraftListing();
        var repository = new StubMarketListingRepository(
            listing,
            saveAccepted: false);
        var useCase = new UpdateListingUseCase(
            repository,
            new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(
            listing.Id,
            listing.PublisherUserId,
            HeadlinePatch(listing.Version, "Updated"));

        Assert.True(result.IsFailure);
        Assert.Equal(
            UpdateListingErrors.ConcurrentModification,
            result.Error);
        Assert.Equal(listing.Version, repository.LastExpectedVersion);
    }

    private static UpdateListingPatch HeadlinePatch(
        long version,
        string headline) =>
        new(
            version,
            false,
            null,
            true,
            new ListingHeadline(headline),
            false,
            null,
            false,
            null);

    private static MarketListing CreateDraftListing() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

    private static MarketListing CreateReadyListing()
    {
        MarketListing listing = CreateDraftListing();
        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Ready"));
        listing.SetPrice(ListingPrice.OnRequest());
        return listing;
    }

    private sealed class StubMarketListingRepository(
        MarketListing? storedListing,
        bool saveAccepted = true)
        : IMarketListingRepository
    {
        public MarketListing? SavedListing { get; private set; }

        public long? LastExpectedVersion { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                storedListing?.Id == listingId
                    ? storedListing
                    : null);

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
            CancellationToken cancellationToken = default)
        {
            SavedListing = listing;
            LastExpectedVersion = expectedVersion;
            return Task.FromResult(saveAccepted);
        }
    }
}
