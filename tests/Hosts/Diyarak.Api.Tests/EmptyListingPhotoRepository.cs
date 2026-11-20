using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;

namespace Diyarak.Api.Tests;

/// <summary>Photo repository with optional seeded photos and no database access.</summary>
internal sealed class EmptyListingPhotoRepository : IListingPhotoRepository
{
    public List<ListingPhoto> Photos { get; } = [];

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
        if (Photos.Any(existing => existing.ListingId == photo.ListingId && existing.Position == photo.Position))
            return Task.FromResult(false);

        Photos.Add(photo);
        return Task.FromResult(true);
    }

    public Task DeleteAsync(ListingPhoto photo, CancellationToken cancellationToken = default)
    {
        Photos.RemoveAll(existing => existing.Id == photo.Id);
        return Task.CompletedTask;
    }
}
