using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class GetPropertyErrors
{
    public static Error InvalidIdentifier { get; } =
        Error.Validation(
            "market.property.invalid_id",
            "The Property identifier must be a non-empty GUID.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.property.not_found",
            "The Property does not exist.");
}
