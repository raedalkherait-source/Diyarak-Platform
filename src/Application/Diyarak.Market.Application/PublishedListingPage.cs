using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed record PublishedListingPage(
    IReadOnlyList<MarketListing> Items,
    int Page,
    int PageSize,
    bool HasMore);
