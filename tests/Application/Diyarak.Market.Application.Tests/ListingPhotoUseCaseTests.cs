using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListingPhotoUseCaseTests
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Stranger = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly FakeListingRepository _listings = new();
    private readonly FakePhotoRepository _photos = new();
    private readonly FakePhotoStorage _storage = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task Upload_sanitizes_stores_and_records_photo_at_next_position()
    {
        MarketListing listing = AddListing(published: false);

        Result<ListingPhoto> first = await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg"));
        Result<ListingPhoto> second = await UploadAsync(listing.Id, Owner, Fixture("text-exif.png"));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, first.Value.Position);
        Assert.Equal(1, second.Value.Position);
        Assert.Equal(ListingPhotoFormat.Jpeg, first.Value.Format);
        Assert.Equal(_clock.UtcNow, first.Value.UploadedAt);
        Assert.Equal(
            $"listings/{listing.Id:N}/{first.Value.Id:N}.jpg",
            first.Value.StorageKey);

        byte[] stored = _storage.Objects[first.Value.StorageKey].Content;
        Assert.Equal(stored.Length, first.Value.SizeBytes);
        Assert.Equal("image/jpeg", _storage.Objects[first.Value.StorageKey].ContentType);
        Assert.Equal(-1, stored.AsSpan().IndexOf("DiyarakTestCam"u8));
    }

    [Fact]
    public async Task Upload_conceals_missing_and_non_owned_listings()
    {
        MarketListing listing = AddListing(published: false);

        Assert.Equal(
            ListingPhotoErrors.ListingNotFound,
            (await UploadAsync(listing.Id, Stranger, Fixture("exif-gps.jpg"))).Error);
        Assert.Equal(
            ListingPhotoErrors.ListingNotFound,
            (await UploadAsync(Guid.NewGuid(), Owner, Fixture("exif-gps.jpg"))).Error);
        Assert.Empty(_storage.Objects);
    }

    [Fact]
    public async Task Upload_rejects_published_listing()
    {
        MarketListing listing = AddListing(published: true);

        Assert.Equal(
            ListingPhotoErrors.ListingNotEditable,
            (await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg"))).Error);
    }

    [Fact]
    public async Task Upload_rejects_unsupported_malformed_empty_and_oversized_content()
    {
        MarketListing listing = AddListing(published: false);

        Assert.Equal(
            ListingPhotoErrors.UnsupportedFormat,
            (await UploadAsync(listing.Id, Owner, "<svg/>"u8.ToArray())).Error);
        Assert.Equal(
            ListingPhotoErrors.InvalidImage,
            (await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg")[..30])).Error);
        Assert.Equal(
            ListingPhotoErrors.Empty,
            (await UploadAsync(listing.Id, Owner, [])).Error);

        byte[] oversized = new byte[ListingPhoto.MaximumSizeBytes + 1];
        Fixture("exif-gps.jpg").CopyTo(oversized, 0);
        Assert.Equal(
            ListingPhotoErrors.TooLarge,
            (await UploadAsync(listing.Id, Owner, oversized)).Error);

        Assert.Empty(_storage.Objects);
        Assert.Empty(_photos.Photos);
    }

    [Fact]
    public async Task Upload_rejects_beyond_photo_limit()
    {
        MarketListing listing = AddListing(published: false);

        for (int i = 0; i < ListingPhoto.MaximumPhotosPerListing; i++)
            Assert.True((await UploadAsync(listing.Id, Owner, Fixture("text-exif.png"))).IsSuccess);

        Assert.Equal(
            ListingPhotoErrors.LimitReached,
            (await UploadAsync(listing.Id, Owner, Fixture("text-exif.png"))).Error);
    }

    [Fact]
    public async Task Upload_removes_stored_object_when_position_is_taken_concurrently()
    {
        MarketListing listing = AddListing(published: false);
        _photos.RejectNextAdd = true;

        Result<ListingPhoto> result = await UploadAsync(listing.Id, Owner, Fixture("exif-xmp.webp"));

        Assert.Equal(ListingPhotoErrors.ConcurrentModification, result.Error);
        Assert.Empty(_storage.Objects);
    }

    [Fact]
    public async Task Delete_removes_record_and_object_for_owner_of_draft()
    {
        MarketListing listing = AddListing(published: false);
        ListingPhoto photo = (await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg"))).Value;
        var useCase = new DeleteListingPhotoUseCase(_listings, _photos, _storage);

        Assert.Equal(
            ListingPhotoErrors.ListingNotFound,
            (await useCase.ExecuteAsync(listing.Id, photo.Id, Stranger)).Error);

        Assert.True((await useCase.ExecuteAsync(listing.Id, photo.Id, Owner)).IsSuccess);
        Assert.Empty(_photos.Photos);
        Assert.Empty(_storage.Objects);

        Assert.Equal(
            ListingPhotoErrors.PhotoNotFound,
            (await useCase.ExecuteAsync(listing.Id, photo.Id, Owner)).Error);
    }

    [Fact]
    public async Task List_returns_owner_photos_in_order_and_conceals_others()
    {
        MarketListing listing = AddListing(published: false);
        await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg"));
        await UploadAsync(listing.Id, Owner, Fixture("text-exif.png"));
        var useCase = new ListListingPhotosUseCase(_listings, _photos);

        Result<IReadOnlyList<ListingPhoto>> owned = await useCase.ExecuteAsync(listing.Id, Owner);

        Assert.Equal([0, 1], owned.Value.Select(photo => photo.Position));
        Assert.Equal(
            ListingPhotoErrors.ListingNotFound,
            (await useCase.ExecuteAsync(listing.Id, Stranger)).Error);
    }

    [Fact]
    public async Task Public_content_requires_published_listing_while_owner_sees_draft()
    {
        MarketListing draft = AddListing(published: false);
        ListingPhoto photo = (await UploadAsync(draft.Id, Owner, Fixture("exif-gps.jpg"))).Value;
        var useCase = new GetListingPhotoContentUseCase(_listings, _photos, _storage);

        Assert.Equal(
            ListingPhotoErrors.PhotoNotFound,
            (await useCase.ExecutePublicAsync(draft.Id, photo.Id)).Error);
        Assert.Equal(
            ListingPhotoErrors.PhotoNotFound,
            (await useCase.ExecuteForOwnerAsync(draft.Id, photo.Id, Stranger)).Error);

        Result<ListingPhotoContent> owner = await useCase.ExecuteForOwnerAsync(draft.Id, photo.Id, Owner);
        Assert.True(owner.IsSuccess);
        await owner.Value.Content.DisposeAsync();

        PublishInPlace(draft);

        Result<ListingPhotoContent> anonymous = await useCase.ExecutePublicAsync(draft.Id, photo.Id);
        Assert.True(anonymous.IsSuccess);
        Assert.Equal(photo.Id, anonymous.Value.Photo.Id);
        await anonymous.Value.Content.DisposeAsync();
    }

    [Fact]
    public async Task Published_listing_projection_includes_photos_in_order()
    {
        MarketListing listing = AddListing(published: false);
        await UploadAsync(listing.Id, Owner, Fixture("exif-gps.jpg"));
        await UploadAsync(listing.Id, Owner, Fixture("text-exif.png"));
        PublishInPlace(listing);

        var properties = new SinglePropertyRepository(listing.SubjectReference.SubjectId);
        var useCase = new GetPublishedListingUseCase(_listings, properties, _photos);

        Result<PublishedListingProjection> result = await useCase.ExecuteAsync(listing.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal([0, 1], result.Value.Photos.Select(photo => photo.Position));
    }

    private Task<Result<ListingPhoto>> UploadAsync(Guid listingId, Guid actor, byte[] content) =>
        new UploadListingPhotoUseCase(_listings, _photos, _storage, _clock)
            .ExecuteAsync(listingId, actor, new MemoryStream(content));

    private MarketListing AddListing(bool published)
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Owner,
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));

        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());

        if (published)
            listing.Publish();

        _listings.Listings[listing.Id] = listing;
        return listing;
    }

    private static void PublishInPlace(MarketListing listing) => listing.Publish();

    private static byte[] Fixture(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeListingRepository : IMarketListingRepository
    {
        public Dictionary<Guid, MarketListing> Listings { get; } = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Listings.GetValueOrDefault(listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. Listings.Values.Where(listing => listing.PublisherUserId == publisherUserId)]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakePhotoRepository : IListingPhotoRepository
    {
        public List<ListingPhoto> Photos { get; } = [];

        public bool RejectNextAdd { get; set; }

        public Task<IReadOnlyList<ListingPhoto>> ListByListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingPhoto>>([.. Photos.Where(photo => photo.ListingId == listingId).OrderBy(photo => photo.Position)]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>> ListByListingsAsync(IReadOnlyCollection<Guid> listingIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>>(
                Photos
                    .Where(photo => listingIds.Contains(photo.ListingId))
                    .GroupBy(photo => photo.ListingId)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<ListingPhoto>)[.. group.OrderBy(photo => photo.Position)]));

        public Task<ListingPhoto?> FindAsync(Guid listingId, Guid photoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Photos.SingleOrDefault(photo => photo.ListingId == listingId && photo.Id == photoId));

        public Task<bool> TryAddAsync(ListingPhoto photo, CancellationToken cancellationToken = default)
        {
            if (RejectNextAdd)
            {
                RejectNextAdd = false;
                return Task.FromResult(false);
            }

            Photos.Add(photo);
            return Task.FromResult(true);
        }

        public Task DeleteAsync(ListingPhoto photo, CancellationToken cancellationToken = default)
        {
            Photos.Remove(photo);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhotoStorage : IListingPhotoStorage
    {
        public Dictionary<string, (byte[] Content, string ContentType)> Objects { get; } = [];

        public Task SaveAsync(string key, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken = default)
        {
            Objects[key] = (content.ToArray(), contentType);
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var item) ? new MemoryStream(item.Content) : null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class SinglePropertyRepository(Guid propertyId) : IMarketPropertyRepository
    {
        public Task<Diyarak.Market.Property.Property?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Diyarak.Market.Property.Property?>(
                id == propertyId
                    ? new Diyarak.Market.Property.Property(
                        propertyId,
                        Diyarak.Market.Property.PropertyCategory.Apartment,
                        new Diyarak.Market.Property.PropertyAddress(null, null, null, "Damascus"))
                    : null);

        public Task<IReadOnlyList<Diyarak.Market.Property.Property>> FindByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Diyarak.Market.Property.Property>>([]);

        public Task AddAsync(Diyarak.Market.Property.Property property, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(Diyarak.Market.Property.Property property, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
