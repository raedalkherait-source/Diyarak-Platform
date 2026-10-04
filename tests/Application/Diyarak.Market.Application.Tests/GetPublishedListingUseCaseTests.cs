using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class GetPublishedListingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_listing_identifier()
    {
        var listingRepository = new StubMarketListingRepository(null);
        var propertyRepository = new StubMarketPropertyRepository(null);
        var useCase = new GetPublishedListingUseCase(
            listingRepository,
            propertyRepository);

        Result<PublishedListingProjection> result =
            await useCase.ExecuteAsync(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.InvalidIdentifier, result.Error);
        Assert.Equal(0, listingRepository.FindCallCount);
        Assert.Equal(0, propertyRepository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_listing_is_missing()
    {
        var listingRepository = new StubMarketListingRepository(null);
        var propertyRepository = new StubMarketPropertyRepository(null);
        var useCase = new GetPublishedListingUseCase(
            listingRepository,
            propertyRepository);

        Result<PublishedListingProjection> result =
            await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.NotFound, result.Error);
        Assert.Equal(1, listingRepository.FindCallCount);
        Assert.Equal(0, propertyRepository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_for_draft_listing()
    {
        MarketListing listing = CreateReadyListing();
        MarketProperty property = CreateProperty(
            listing.SubjectReference.SubjectId);
        var listingRepository = new StubMarketListingRepository(listing);
        var propertyRepository = new StubMarketPropertyRepository(property);
        var useCase = new GetPublishedListingUseCase(
            listingRepository,
            propertyRepository);

        Result<PublishedListingProjection> result =
            await useCase.ExecuteAsync(listing.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.NotFound, result.Error);
        Assert.Equal(1, listingRepository.FindCallCount);
        Assert.Equal(0, propertyRepository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_property_is_missing()
    {
        MarketListing listing = CreateReadyListing();
        listing.Publish();
        var listingRepository = new StubMarketListingRepository(listing);
        var propertyRepository = new StubMarketPropertyRepository(null);
        var useCase = new GetPublishedListingUseCase(
            listingRepository,
            propertyRepository);

        Result<PublishedListingProjection> result =
            await useCase.ExecuteAsync(listing.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GetListingErrors.NotFound, result.Error);
        Assert.Equal(1, listingRepository.FindCallCount);
        Assert.Equal(1, propertyRepository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_published_listing_projection_without_actor_requirement()
    {
        MarketListing listing = CreateReadyListing();
        listing.Publish();
        MarketProperty property = CreateProperty(
            listing.SubjectReference.SubjectId);
        var listingRepository = new StubMarketListingRepository(listing);
        var propertyRepository = new StubMarketPropertyRepository(property);
        var useCase = new GetPublishedListingUseCase(
            listingRepository,
            propertyRepository);

        Result<PublishedListingProjection> result =
            await useCase.ExecuteAsync(listing.Id);

        Assert.True(result.IsSuccess);
        Assert.Same(listing, result.Value.Listing);
        Assert.Same(property, result.Value.Property);
        Assert.Equal(1, listingRepository.FindCallCount);
        Assert.Equal(1, propertyRepository.FindCallCount);
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

    private static MarketProperty CreateProperty(Guid propertyId) =>
        new(
            propertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Test Street",
                "1",
                "10115",
                "Berlin"));

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

    private sealed class StubMarketPropertyRepository(
        MarketProperty? property)
        : IMarketPropertyRepository
    {
        public int FindCallCount { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;

            return Task.FromResult(
                property?.Id == propertyId
                    ? property
                    : null);
        }

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>(
                Array.Empty<MarketProperty>());

        public Task<IReadOnlyList<MarketProperty>> FindPageByOwnerUserIdAsync(
            Guid ownerUserId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>(
                Array.Empty<MarketProperty>());

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(
            MarketProperty property,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}