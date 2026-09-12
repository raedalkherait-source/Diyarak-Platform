using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class CreateListingErrors
{
    public static Error InvalidPropertyIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_property_id",
            "The Property identifier must be a non-empty GUID.");

    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");

    public static Error PropertyNotFound { get; } =
        Error.Conflict(
            "market.listing.property_not_found",
            "The referenced Property does not exist.");
}
