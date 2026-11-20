using Diyarak.Market.Listing;

namespace Diyarak.Market.Application.Photos;

public interface IListingPhotoRepository
{
    public Task<IReadOnlyList<ListingPhoto>> ListByListingAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>> ListByListingsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default);

    public Task<ListingPhoto?> FindAsync(
        Guid listingId,
        Guid photoId,
        CancellationToken cancellationToken = default);

    /// <returns><c>false</c> when another photo already occupies the position.</returns>
    public Task<bool> TryAddAsync(
        ListingPhoto photo,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        ListingPhoto photo,
        CancellationToken cancellationToken = default);
}

/// <summary>Stores photo binaries by opaque key (local disk, S3-compatible object storage, ...).</summary>
public interface IListingPhotoStorage
{
    public Task SaveAsync(
        string key,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <returns><c>null</c> when no object exists for the key.</returns>
    public Task<Stream?> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);
}

public sealed record ListingPhotoContent(
    ListingPhoto Photo,
    Stream Content);
