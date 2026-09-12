using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public interface IMarketListingRepository
{
    public Task<MarketListing?> FindByIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    public Task AddAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default);

    public Task SaveAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default);
}
