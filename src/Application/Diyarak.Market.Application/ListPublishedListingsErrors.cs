using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public static class ListPublishedListingsErrors
{
    public static Error InvalidPagination { get; } =
        Error.Validation(
            "market.listing.invalid_public_pagination",
            "The public Listing page must be positive and pageSize must be between 1 and 100.");

    public static Error InvalidSearchCriteria { get; } =
        Error.Validation(
            "market.listing.invalid_public_search_criteria",
            "The public Listing search criteria are invalid.");
}
