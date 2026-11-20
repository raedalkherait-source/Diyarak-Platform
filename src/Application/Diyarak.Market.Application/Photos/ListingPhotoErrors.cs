using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Photos;

public static class ListingPhotoErrors
{
    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.listing_photo.invalid_id",
            "The Listing and photo identifiers must be non-empty GUIDs.");

    public static Error ListingNotFound { get; } =
        Error.NotFound(
            "market.listing_photo.listing_not_found",
            "The Listing does not exist.");

    public static Error PhotoNotFound { get; } =
        Error.NotFound(
            "market.listing_photo.not_found",
            "The photo does not exist.");

    public static Error ListingNotEditable { get; } =
        Error.Conflict(
            "market.listing_photo.listing_not_editable",
            "Photos of a Closed Listing cannot be changed.");

    public static Error LimitReached { get; } =
        Error.Conflict(
            "market.listing_photo.limit_reached",
            $"A Listing can have at most {ListingPhoto.MaximumPhotosPerListing} photos.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.listing_photo.concurrent_modification",
            "Another photo was added at the same time. Try again.");

    public static Error Empty { get; } =
        Error.Validation(
            "market.listing_photo.empty",
            "The photo content is empty.");

    public static Error TooLarge { get; } =
        Error.Validation(
            "market.listing_photo.too_large",
            $"A photo can be at most {ListingPhoto.MaximumSizeBytes / (1024 * 1024)} MB.");

    public static Error UnsupportedFormat { get; } =
        Error.Validation(
            "market.listing_photo.unsupported_format",
            "Only JPEG, PNG and WebP photos are supported.");

    public static Error InvalidImage { get; } =
        Error.Validation(
            "market.listing_photo.invalid_image",
            "The photo content is not a valid image.");
}
