using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Photos;

public sealed class UploadListingPhotoUseCase(
    IMarketListingRepository listings,
    IListingPhotoRepository photos,
    IListingPhotoStorage storage,
    IClock clock)
{
    public async Task<Result<ListingPhoto>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        Result<MarketListing> owned = await ListingPhotoAccess.LoadOwnedEditableAsync(
            listings, listingId, actorUserId, cancellationToken);

        if (!owned.IsSuccess)
            return Result.Failure<ListingPhoto>(owned.Error);

        IReadOnlyList<ListingPhoto> existing =
            await photos.ListByListingAsync(listingId, cancellationToken);

        if (existing.Count >= ListingPhoto.MaximumPhotosPerListing)
            return Result.Failure<ListingPhoto>(ListingPhotoErrors.LimitReached);

        byte[]? raw = await ReadBoundedAsync(content, ListingPhoto.MaximumSizeBytes, cancellationToken);

        if (raw is null)
            return Result.Failure<ListingPhoto>(ListingPhotoErrors.TooLarge);

        if (raw.Length == 0)
            return Result.Failure<ListingPhoto>(ListingPhotoErrors.Empty);

        switch (ListingPhotoSanitizer.Sanitize(raw, out ListingPhotoFormat format, out byte[] sanitized))
        {
            case ListingPhotoSanitizeStatus.UnsupportedFormat:
                return Result.Failure<ListingPhoto>(ListingPhotoErrors.UnsupportedFormat);
            case ListingPhotoSanitizeStatus.Malformed:
                return Result.Failure<ListingPhoto>(ListingPhotoErrors.InvalidImage);
        }

        Guid photoId = Guid.NewGuid();
        int position = existing.Count == 0 ? 0 : existing.Max(photo => photo.Position) + 1;

        if (position >= ListingPhoto.MaximumPhotosPerListing)
            return Result.Failure<ListingPhoto>(ListingPhotoErrors.LimitReached);

        var photo = new ListingPhoto(
            photoId,
            listingId,
            ListingPhoto.CreateStorageKey(listingId, photoId, format),
            format,
            sanitized.Length,
            position,
            clock.UtcNow);

        await storage.SaveAsync(photo.StorageKey, sanitized, format.ToContentType(), cancellationToken);

        if (!await photos.TryAddAsync(photo, cancellationToken))
        {
            await storage.DeleteAsync(photo.StorageKey, CancellationToken.None);
            return Result.Failure<ListingPhoto>(ListingPhotoErrors.ConcurrentModification);
        }

        return Result.Success(photo);
    }

    /// <returns><c>null</c> when the stream is longer than <paramref name="maximumBytes"/>.</returns>
    private static async Task<byte[]?> ReadBoundedAsync(
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];

        while (true)
        {
            int read = await content.ReadAsync(chunk, cancellationToken);

            if (read == 0)
                return buffer.ToArray();

            if (buffer.Length + read > maximumBytes)
                return null;

            buffer.Write(chunk, 0, read);
        }
    }
}

public sealed class ListListingPhotosUseCase(
    IMarketListingRepository listings,
    IListingPhotoRepository photos)
{
    public async Task<Result<IReadOnlyList<ListingPhoto>>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Result<MarketListing> owned = await ListingPhotoAccess.LoadOwnedAsync(
            listings, listingId, actorUserId, cancellationToken);

        return owned.IsSuccess
            ? Result.Success(await photos.ListByListingAsync(listingId, cancellationToken))
            : Result.Failure<IReadOnlyList<ListingPhoto>>(owned.Error);
    }
}

public sealed class DeleteListingPhotoUseCase(
    IMarketListingRepository listings,
    IListingPhotoRepository photos,
    IListingPhotoStorage storage)
{
    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid photoId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (photoId == Guid.Empty)
            return Result.Failure(ListingPhotoErrors.InvalidIdentifier);

        Result<MarketListing> owned = await ListingPhotoAccess.LoadOwnedEditableAsync(
            listings, listingId, actorUserId, cancellationToken);

        if (!owned.IsSuccess)
            return Result.Failure(owned.Error);

        ListingPhoto? photo = await photos.FindAsync(listingId, photoId, cancellationToken);

        if (photo is null)
            return Result.Failure(ListingPhotoErrors.PhotoNotFound);

        await photos.DeleteAsync(photo, cancellationToken);
        await storage.DeleteAsync(photo.StorageKey, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Loads photo content for the Listing owner (any status) or for anyone when Published.</summary>
public sealed class GetListingPhotoContentUseCase(
    IMarketListingRepository listings,
    IListingPhotoRepository photos,
    IListingPhotoStorage storage)
{
    public Task<Result<ListingPhotoContent>> ExecuteForOwnerAsync(
        Guid listingId,
        Guid photoId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(listingId, photoId, actorUserId, cancellationToken);

    public Task<Result<ListingPhotoContent>> ExecutePublicAsync(
        Guid listingId,
        Guid photoId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(listingId, photoId, actorUserId: null, cancellationToken);

    private async Task<Result<ListingPhotoContent>> ExecuteAsync(
        Guid listingId,
        Guid photoId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (listingId == Guid.Empty || photoId == Guid.Empty || actorUserId == Guid.Empty)
            return Result.Failure<ListingPhotoContent>(ListingPhotoErrors.InvalidIdentifier);

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);

        bool visible = listing is not null &&
            (actorUserId is { } actor
                ? listing.PublisherUserId == actor
                : listing.IsPubliclyVisible);

        if (!visible)
            return Result.Failure<ListingPhotoContent>(ListingPhotoErrors.PhotoNotFound);

        ListingPhoto? photo = await photos.FindAsync(listingId, photoId, cancellationToken);

        if (photo is null)
            return Result.Failure<ListingPhotoContent>(ListingPhotoErrors.PhotoNotFound);

        Stream? content = await storage.OpenReadAsync(photo.StorageKey, cancellationToken);

        return content is null
            ? Result.Failure<ListingPhotoContent>(ListingPhotoErrors.PhotoNotFound)
            : Result.Success(new ListingPhotoContent(photo, content));
    }
}

internal static class ListingPhotoAccess
{
    internal static async Task<Result<MarketListing>> LoadOwnedAsync(
        IMarketListingRepository listings,
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty)
            return Result.Failure<MarketListing>(ListingPhotoErrors.InvalidIdentifier);

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);

        // Missing and non-owned Listings are indistinguishable (ADR-0023).
        return listing is null || listing.PublisherUserId != actorUserId
            ? Result.Failure<MarketListing>(ListingPhotoErrors.ListingNotFound)
            : Result.Success(listing);
    }

    internal static async Task<Result<MarketListing>> LoadOwnedEditableAsync(
        IMarketListingRepository listings,
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        Result<MarketListing> owned = await LoadOwnedAsync(listings, listingId, actorUserId, cancellationToken);

        return owned.IsSuccess && !owned.Value.IsEditable
            ? Result.Failure<MarketListing>(ListingPhotoErrors.ListingNotEditable)
            : owned;
    }
}
