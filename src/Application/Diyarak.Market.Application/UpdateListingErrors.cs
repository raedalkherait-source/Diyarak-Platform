using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class UpdateListingErrors
{
    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_id",
            "The Listing identifier must be a non-empty GUID.");

    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");

    public static Error InvalidPatch { get; } =
        Error.Validation(
            "market.listing.invalid_patch",
            "The Listing update request is invalid.");

    public static Error NoChanges { get; } =
        Error.Validation(
            "market.listing.no_changes",
            "The Listing update must contain at least one editable field.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing.not_found",
            "The Listing does not exist.");

    public static Error CannotEdit { get; } =
        Error.Conflict(
            "market.listing.cannot_edit",
            "Only a Draft Listing can be edited.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.listing.concurrent_modification",
            "The Listing changed after the supplied version was read.");
}
