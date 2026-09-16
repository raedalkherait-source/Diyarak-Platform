using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class ListOwnedPropertiesErrors
{
    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");

    public static Error InvalidPagination { get; } =
        Error.Validation(
            "market.property.invalid_pagination",
            "The management Property page must be positive and pageSize must be between 1 and 100.");
}
