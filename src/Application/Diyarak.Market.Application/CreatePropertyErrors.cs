using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class CreatePropertyErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.property.invalid_create_request",
            "The Property creation request is invalid.");
}
