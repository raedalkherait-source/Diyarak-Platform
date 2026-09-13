using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class CreatePropertyErrors
{
    public static Error InvalidActorIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_actor_id",
            "The authenticated Property owner identifier must be non-empty.");

    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.property.invalid_create_request",
            "The Property creation request is invalid.");
}
