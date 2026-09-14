using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class ListOwnedPropertiesErrors
{
    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");
}
