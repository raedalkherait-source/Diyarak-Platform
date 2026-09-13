using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class GetListingErrors
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
}
