using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class GetPropertyErrors
{
    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_actor_id",
            "The authenticated Property owner identifier must be non-empty.");

    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_id",
            "The Property identifier must be a non-empty GUID.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.property.not_found",
            "The Property does not exist or is not owned by the authenticated user.");
}
