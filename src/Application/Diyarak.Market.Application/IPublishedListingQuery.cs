using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public interface IPublishedListingQuery
{
    public Task<IReadOnlyList<MarketListing>> ListPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
