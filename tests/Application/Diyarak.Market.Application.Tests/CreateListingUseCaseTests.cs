using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class CreateListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_property_identifier()
    {
        var repository = new StubMarketListingRepository();
        var checker = new StubPropertyExistenceChecker(exists: true);
        var useCase = new CreateListingUseCase(repository, checker);

        Result<Guid> result = await useCase.ExecuteAsync(
            Guid.Empty,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreateListingErrors.InvalidPropertyIdentifier,
            result.Error);
        Assert.Equal(0, checker.CallCount);
        Assert.Null(repository.AddedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository = new StubMarketListingRepository();
        var checker = new StubPropertyExistenceChecker(exists: true);
        var useCase = new CreateListingUseCase(repository, checker);

        Result<Guid> result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreateListingErrors.InvalidActorIdentifier,
            result.Error);
        Assert.Equal(0, checker.CallCount);
        Assert.Null(repository.AddedListing);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_when_property_does_not_exist()
    {
        var repository = new StubMarketListingRepository();
        var checker = new StubPropertyExistenceChecker(exists: false);
        var useCase = new CreateListingUseCase(repository, checker);
        Guid propertyId = Guid.NewGuid();

        Result<Guid> result = await useCase.ExecuteAsync(
            propertyId,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreateListingErrors.PropertyNotFound,
            result.Error);
        Assert.Equal(propertyId, checker.LastPropertyId);
        Assert.Null(repository.AddedListing);
    }

    [Fact]
    public async Task ExecuteAsync_creates_and_adds_draft_owned_by_actor()
    {
        var repository = new StubMarketListingRepository();
        var checker = new StubPropertyExistenceChecker(exists: true);
        var useCase = new CreateListingUseCase(repository, checker);
        Guid propertyId = Guid.NewGuid();
        Guid actorUserId = Guid.NewGuid();

        Result<Guid> result = await useCase.ExecuteAsync(
            propertyId,
            actorUserId);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(propertyId, checker.LastPropertyId);

        MarketListing listing = Assert.IsType<MarketListing>(
            repository.AddedListing);

        Assert.Equal(result.Value, listing.Id);
        Assert.Equal(actorUserId, listing.PublisherUserId);
        Assert.Equal(propertyId, listing.SubjectReference.SubjectId);
        Assert.Equal(
            MarketListingSubjectTypes.Property,
            listing.SubjectReference.SubjectType);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Null(listing.Context);
        Assert.Null(listing.Headline);
        Assert.Null(listing.Price);
        Assert.Null(listing.AvailableFromDate);
    }

    private sealed class StubMarketListingRepository
        : IMarketListingRepository
    {
        public MarketListing? AddedListing { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketListing?>(null);

        public Task AddAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default)
        {
            AddedListing = listing;
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
