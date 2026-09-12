using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class PublishListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_listing_identifier()
    {
        var repository =
            new StubMarketListingRepository(storedListing: null);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var transactionRunner =
            new StubMarketTransactionRunner();

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                transactionRunner);

        Result result =
            await useCase.ExecuteAsync(Guid.Empty, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            PublishListingErrors.InvalidIdentifier,
            result.Error);
        Assert.Null(repository.LastRequestedId);
        Assert.Null(repository.SavedListing);
        Assert.Equal(0, checker.CallCount);
        Assert.Equal(0, transactionRunner.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository =
            new StubMarketListingRepository(storedListing: null);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var transactionRunner =
            new StubMarketTransactionRunner();

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                transactionRunner);

        Result result =
            await useCase.ExecuteAsync(
                Guid.NewGuid(),
                Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            PublishListingErrors.InvalidActorIdentifier,
            result.Error);
        Assert.Null(repository.LastRequestedId);
        Assert.Null(repository.SavedListing);
        Assert.Equal(0, checker.CallCount);
        Assert.Equal(0, transactionRunner.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_listing_does_not_exist()
    {
        var repository =
            new StubMarketListingRepository(storedListing: null);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                new StubMarketTransactionRunner());

        Guid listingId = Guid.NewGuid();
        Guid actorUserId = Guid.NewGuid();

        Result result =
            await useCase.ExecuteAsync(listingId, actorUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(PublishListingErrors.NotFound, result.Error);
        Assert.Equal(listingId, repository.LastRequestedId);
        Assert.Null(repository.SavedListing);
        Assert.Equal(0, checker.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_actor_does_not_own_listing()
    {
        MarketListing listing = CreateReadyListing();
        Guid actorUserId = Guid.NewGuid();

        Assert.NotEqual(
            listing.PublisherUserId,
            actorUserId);

        var repository =
            new StubMarketListingRepository(listing);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                new StubMarketTransactionRunner());

        Result result =
            await useCase.ExecuteAsync(
                listing.Id,
                actorUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            PublishListingErrors.NotFound,
            result.Error);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(listing.Id, repository.LastRequestedId);
        Assert.Null(repository.SavedListing);
        Assert.Equal(0, checker.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_returns_conflict_when_property_does_not_exist()
    {
        MarketListing listing = CreateReadyListing();

        var repository =
            new StubMarketListingRepository(listing);

        var checker =
            new StubPropertyExistenceChecker(exists: false);

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                new StubMarketTransactionRunner());

        Result result =
            await useCase.ExecuteAsync(
                listing.Id,
                listing.PublisherUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            PublishListingErrors.PropertyNotFound,
            result.Error);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(
            listing.SubjectReference.SubjectId,
            checker.LastPropertyId);
        Assert.Null(repository.SavedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_when_listing_cannot_be_published()
    {
        MarketListing listing = CreateDraftListing();

        var repository =
            new StubMarketListingRepository(listing);

        var checker =
            new StubPropertyExistenceChecker(exists: true);

        var useCase =
            new PublishListingUseCase(
                repository,
                checker,
                new StubMarketTransactionRunner());

        Result result =
            await useCase.ExecuteAsync(
                listing.Id,
                listing.PublisherUserId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            PublishListingErrors.CannotPublish,
            result.Error);
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
            new PublishListingUseCase(
                repository,
                checker,
                new StubMarketTransactionRunner());

        Result result =
            await useCase.ExecuteAsync(
                listing.Id,
                listing.PublisherUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(
            listing.SubjectReference.SubjectId,
            checker.LastPropertyId);
        Assert.Same(listing, repository.SavedListing);
    }

    private static MarketListing CreateDraftListing()
    {
        var subjectReference = new ListingSubjectReference(
            Guid.NewGuid(),
            MarketListingSubjectTypes.Property);

        return new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            subjectReference);
    }

    private static MarketListing CreateReadyListing()
    {
        MarketListing listing = CreateDraftListing();

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

        public Task AddAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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
