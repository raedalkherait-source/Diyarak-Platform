using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class UpdatePropertyErrors
{
    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_id",
            "The Property identifier must be a non-empty GUID.");

    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_actor_id",
            "The actor user identifier must be a non-empty GUID.");

    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.property.invalid_update",
            "The Property replacement request is invalid.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.property.not_found",
            "The Property does not exist.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.property.concurrent_modification",
            "The Property changed after the supplied version was read.");
}
