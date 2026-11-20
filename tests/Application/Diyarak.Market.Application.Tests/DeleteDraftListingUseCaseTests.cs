using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class DeleteDraftListingUseCaseTests
{
    [Fact]
    public async Task Owner_deletes_draft_photos_and_unreferenced_property()
    {
        MarketListing draft = CreateDraft();
        var store = new DeletionStore(["listings/a.jpg", "listings/b.webp"]);
        var storage = new Storage();
        DeleteDraftListingUseCase useCase = Create(draft, store, storage);

        Result result = await useCase.ExecuteAsync(draft.Id, draft.PublisherUserId, draft.Version);

        Assert.True(result.IsSuccess);
        Assert.Equal((draft.Id, draft.Version), store.DeletedListing);
        Assert.Equal(draft.Id, store.PhotosDeletedFor);
        Assert.Equal((draft.SubjectReference.SubjectId, draft.PublisherUserId), store.PropertyCheck);
        Assert.Equal(["listings/a.jpg", "listings/b.webp"], storage.Deleted);
    }

    [Fact]
    public async Task Non_owner_gets_concealed_not_found_and_nothing_is_deleted()
    {
        MarketListing draft = CreateDraft();
        var store = new DeletionStore([]);
        DeleteDraftListingUseCase useCase = Create(draft, store, new Storage());

        Result result = await useCase.ExecuteAsync(draft.Id, Guid.NewGuid(), draft.Version);

        Assert.Equal(DeleteDraftListingErrors.NotFound, result.Error);
        Assert.Null(store.DeletedListing);
    }

    [Fact]
    public async Task Published_listing_cannot_be_deleted()
    {
        MarketListing listing = CreateDraft();
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();
        var store = new DeletionStore([]);

        Result result = await Create(listing, store, new Storage()).ExecuteAsync(listing.Id, listing.PublisherUserId, listing.Version);

        Assert.Equal(DeleteDraftListingErrors.NotDraft, result.Error);
        Assert.Null(store.DeletedListing);
    }

    [Fact]
    public async Task Stale_version_or_lost_race_is_a_conflict_and_keeps_files()
    {
        MarketListing draft = CreateDraft();
        var storage = new Storage();

        Result stale = await Create(draft, new DeletionStore(["k"]), storage)
            .ExecuteAsync(draft.Id, draft.PublisherUserId, draft.Version + 1);
        Assert.Equal(DeleteDraftListingErrors.ConcurrentModification, stale.Error);

        Result raced = await Create(draft, new DeletionStore(["k"], deleteAccepted: false), storage)
            .ExecuteAsync(draft.Id, draft.PublisherUserId, draft.Version);
        Assert.Equal(DeleteDraftListingErrors.ConcurrentModification, raced.Error);

        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task Invalid_requests_are_rejected()
    {
        MarketListing draft = CreateDraft();
        DeleteDraftListingUseCase useCase = Create(draft, new DeletionStore([]), new Storage());

        Assert.Equal(DeleteDraftListingErrors.InvalidRequest, (await useCase.ExecuteAsync(Guid.Empty, draft.PublisherUserId, 1)).Error);
        Assert.Equal(DeleteDraftListingErrors.InvalidRequest, (await useCase.ExecuteAsync(draft.Id, Guid.Empty, 1)).Error);
        Assert.Equal(DeleteDraftListingErrors.InvalidRequest, (await useCase.ExecuteAsync(draft.Id, draft.PublisherUserId, 0)).Error);
    }

    private static DeleteDraftListingUseCase Create(MarketListing listing, DeletionStore store, Storage storage) =>
        new(new Repository(listing), storage, store, new StubMarketTransactionRunner());

    private static MarketListing CreateDraft() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));

    private sealed class DeletionStore(IReadOnlyList<string> photoKeys, bool deleteAccepted = true) : IDraftListingDeletionStore
    {
        public (Guid ListingId, long Version)? DeletedListing { get; private set; }

        public Guid? PhotosDeletedFor { get; private set; }

        public (Guid PropertyId, Guid OwnerUserId)? PropertyCheck { get; private set; }

        public Task<bool> TryDeleteListingAsync(Guid listingId, long expectedVersion, CancellationToken cancellationToken = default)
        {
            if (deleteAccepted)
                DeletedListing = (listingId, expectedVersion);

            return Task.FromResult(deleteAccepted);
        }

        public Task<IReadOnlyList<string>> DeletePhotosAsync(Guid listingId, CancellationToken cancellationToken = default)
        {
            PhotosDeletedFor = listingId;
            return Task.FromResult(photoKeys);
        }

        public Task DeletePropertyIfUnreferencedAsync(Guid propertyId, Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            PropertyCheck = (propertyId, ownerUserId);
            return Task.CompletedTask;
        }
    }

    private sealed class Storage : IListingPhotoStorage
    {
        public List<string> Deleted { get; } = [];

        public Task SaveAsync(string key, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Deleted.Add(key);
            return Task.CompletedTask;
        }
    }

    private sealed class Repository(MarketListing listing) : IMarketListingRepository
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
