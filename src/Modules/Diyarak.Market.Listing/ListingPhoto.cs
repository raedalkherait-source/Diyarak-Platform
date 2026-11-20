using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

/// <summary>
/// A photo attached to a Listing. The binary content lives in external storage under
/// <see cref="StorageKey"/>; the aggregate holds only immutable metadata and display position.
/// </summary>
public sealed class ListingPhoto : AggregateRoot<Guid>
{
    public const long MaximumSizeBytes = 10 * 1024 * 1024;

    public const int MaximumPhotosPerListing = 30;

    public ListingPhoto(
        Guid id,
        Guid listingId,
        string storageKey,
        ListingPhotoFormat format,
        long sizeBytes,
        int position,
        DateTimeOffset uploadedAt)
        : base(id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Photo identifier cannot be empty.", nameof(id));

        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing identifier cannot be empty.", nameof(listingId));

        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported photo format.");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeBytes, MaximumSizeBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, MaximumPhotosPerListing);

        ListingId = listingId;
        StorageKey = storageKey;
        Format = format;
        SizeBytes = sizeBytes;
        Position = position;
        UploadedAt = uploadedAt;
    }

    public Guid ListingId { get; }

    public string StorageKey { get; }

    public ListingPhotoFormat Format { get; }

    public long SizeBytes { get; }

    /// <summary>Zero-based display order; position <c>0</c> is the cover photo.</summary>
    public int Position { get; }

    public DateTimeOffset UploadedAt { get; }

    public static string CreateStorageKey(Guid listingId, Guid photoId, ListingPhotoFormat format) =>
        $"listings/{listingId:N}/{photoId:N}.{format.ToFileExtension()}";
}
