using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class PublishListingErrors
{
    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_id",
            "The Listing identifier must be a non-empty GUID.");

    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing.not_found",
            "The Listing does not exist.");

    public static Error PropertyNotFound { get; } =
        Error.Conflict(
            "market.listing.property_not_found",
            "The referenced Property does not exist.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.listing.concurrent_modification",
            "The Listing changed while publication was being attempted.");

    public static Error CannotPublish { get; } =
        Error.Conflict(
            "market.listing.cannot_publish",
            "The Listing cannot be published in its current state.");
}
