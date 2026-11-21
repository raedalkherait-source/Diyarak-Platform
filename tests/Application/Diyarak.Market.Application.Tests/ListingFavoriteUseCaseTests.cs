using Diyarak.Market.Application.Favorites;
using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class ListingFavoriteUseCaseTests
{
    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public async Task Saving_a_published_listing_is_idempotent()
    {
        MarketListing listing = CreateListing(publish: true);
        var store = new Store();
        AddListingFavoriteUseCase useCase = CreateAdd(store, listing);

        Assert.True((await useCase.ExecuteAsync(listing.Id, User)).IsSuccess);
        Assert.True((await useCase.ExecuteAsync(listing.Id, User)).IsSuccess);

        Assert.Single(store.Items);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero), store.Items.Single().SavedAt);
    }

    [Fact]
    public async Task Drafts_and_missing_listings_cannot_be_saved()
    {
        MarketListing draft = CreateListing(publish: false);
        var store = new Store();
        AddListingFavoriteUseCase useCase = CreateAdd(store, draft);

        Assert.Equal(ListingFavoriteErrors.NotFound, (await useCase.ExecuteAsync(draft.Id, User)).Error);
        Assert.Equal(ListingFavoriteErrors.NotFound, (await useCase.ExecuteAsync(Guid.NewGuid(), User)).Error);
        Assert.Equal(ListingFavoriteErrors.InvalidRequest, (await useCase.ExecuteAsync(draft.Id, Guid.Empty)).Error);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Saving_beyond_the_limit_is_rejected()
    {
        MarketListing listing = CreateListing(publish: true);
        var store = new Store();
        for (int index = 0; index < ListingFavoriteErrors.MaximumFavorites; index++)
            store.Items.Add((User, Guid.NewGuid(), DateTimeOffset.UnixEpoch));

        Result result = await CreateAdd(store, listing).ExecuteAsync(listing.Id, User);

        Assert.Equal(ListingFavoriteErrors.LimitReached, result.Error);
    }

    [Fact]
    public async Task Removing_and_status_are_scoped_to_the_actor()
    {
        Guid listingId = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        var store = new Store();
        store.Items.Add((User, listingId, DateTimeOffset.UnixEpoch));
        store.Items.Add((other, listingId, DateTimeOffset.UnixEpoch));

        Assert.True((await new GetListingFavoriteStatusUseCase(store).ExecuteAsync(listingId, User)).Value);
        Assert.True((await new RemoveListingFavoriteUseCase(store).ExecuteAsync(listingId, User)).IsSuccess);
        Assert.True((await new RemoveListingFavoriteUseCase(store).ExecuteAsync(listingId, User)).IsSuccess);

        Assert.False((await new GetListingFavoriteStatusUseCase(store).ExecuteAsync(listingId, User)).Value);
        Assert.True((await new GetListingFavoriteStatusUseCase(store).ExecuteAsync(listingId, other)).Value);
    }

    [Fact]
    public async Task Listing_pages_through_published_favorites_and_attaches_photos()
    {
        MarketListing first = CreateListing(publish: true);
        MarketListing second = CreateListing(publish: true);
        var store = new Store { Published = [Project(first), Project(second)] };
        var photos = new Photos(first.Id);
        var useCase = new ListListingFavoritesUseCase(store, photos);

        Result<FavoriteListingPage> page = await useCase.ExecuteAsync(User, 1, 1);

        Assert.True(page.IsSuccess);
        Assert.True(page.Value.HasMore);
        Assert.Equal(first.Id, Assert.Single(page.Value.Items).Listing.Id);
        Assert.Single(page.Value.Items[0].Photos);
        Assert.Equal((0, 2), store.LastWindow);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(11, 20)]
    public async Task Invalid_pagination_is_rejected(int page, int pageSize)
    {
        Result<FavoriteListingPage> result =
            await new ListListingFavoritesUseCase(new Store(), new Photos(Guid.Empty)).ExecuteAsync(User, page, pageSize);

        Assert.Equal(ListingFavoriteErrors.InvalidRequest, result.Error);
    }

    private static AddListingFavoriteUseCase CreateAdd(Store store, MarketListing listing) =>
        new(new Listings(listing), store, new StubMarketTransactionRunner(), new Clock());

    private static MarketListing CreateListing(bool publish)
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        if (publish)
            listing.Publish();
        return listing;
    }

    private static PublishedListingProjection Project(MarketListing listing) =>
        new(
            listing,
            new MarketProperty(
                listing.SubjectReference.SubjectId,
                PropertyCategory.Apartment,
                new PropertyAddress("Street", "1", "10115", "Damascus")));

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class Store : IListingFavoriteStore
    {
        public List<(Guid UserId, Guid ListingId, DateTimeOffset SavedAt)> Items { get; } = [];

        public IReadOnlyList<PublishedListingProjection> Published { get; init; } = [];

        public (int Skip, int Take)? LastWindow { get; private set; }

        public Task<bool> AddAsync(Guid userId, Guid listingId, DateTimeOffset savedAt, CancellationToken cancellationToken = default)
        {
            Items.Add((userId, listingId, savedAt));
            return Task.FromResult(true);
        }

        public Task RemoveAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(item => item.UserId == userId && item.ListingId == listingId);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(item => item.UserId == userId));

        public Task<bool> ContainsAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(item => item.UserId == userId && item.ListingId == listingId));

        public Task<int> CountForListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(item => item.ListingId == listingId));

        public Task<IReadOnlyList<PublishedListingProjection>> ListPublishedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            LastWindow = (skip, take);
            return Task.FromResult<IReadOnlyList<PublishedListingProjection>>([.. Published.Skip(skip).Take(take)]);
        }
    }

    private sealed class Photos(Guid listingWithPhoto) : IListingPhotoRepository
    {
        public Task<IReadOnlyList<ListingPhoto>> ListByListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingPhoto>>([]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>> ListByListingsAsync(IReadOnlyCollection<Guid> listingIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>>(
                new Dictionary<Guid, IReadOnlyList<ListingPhoto>>
                {
                    [listingWithPhoto] = [new ListingPhoto(Guid.NewGuid(), listingWithPhoto, "listings/a.jpg", ListingPhotoFormat.Jpeg, 1000, 0, DateTimeOffset.UnixEpoch)],
                });

        public Task<ListingPhoto?> FindAsync(Guid listingId, Guid photoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ListingPhoto?>(null);

        public Task<bool> TryAddAsync(ListingPhoto photo, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task DeleteAsync(ListingPhoto photo, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class Listings(MarketListing listing) : IMarketListingRepository
    {
        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(listing.Id == listingId ? listing : null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([listing]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
