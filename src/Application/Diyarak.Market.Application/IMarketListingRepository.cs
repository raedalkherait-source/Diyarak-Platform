using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public interface IMarketListingRepository
{
    public Task<MarketListing?> FindByIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(
        Guid publisherUserId,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<MarketListing>> FindPageByPublisherUserIdAsync(
        Guid publisherUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IReadOnlyList<MarketListing> listings =
            await FindByPublisherUserIdAsync(
                publisherUserId,
                cancellationToken);

        return listings
            .OrderBy(listing => listing.Id)
            .Skip(skip)
            .Take(take)
            .ToArray();
    }

    public Task AddAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default);

    public Task<bool> TrySaveAsync(
        MarketListing listing,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}
