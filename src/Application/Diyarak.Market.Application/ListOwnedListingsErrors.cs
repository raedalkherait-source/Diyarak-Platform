using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class ListOwnedListingsErrors
{
    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.listing.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");
}
