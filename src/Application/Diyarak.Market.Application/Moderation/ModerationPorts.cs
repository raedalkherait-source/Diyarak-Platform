using Diyarak.Market.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Moderation;

public interface IListingReportRepository
{
    public Task AddAsync(ListingReport report, CancellationToken cancellationToken = default);

    /// <summary>Open reports, oldest first.</summary>
    public Task<IReadOnlyList<ListingReport>> ListOpenAsync(int skip, int take, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ListingReport>> ListOpenByListingAsync(Guid listingId, CancellationToken cancellationToken = default);

    public Task<int> CountRecentAsync(Guid listingId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>Persists resolution changes made to previously loaded reports.</summary>
    public Task SaveResolutionsAsync(IReadOnlyCollection<ListingReport> reports, CancellationToken cancellationToken = default);
}

public interface IListingModerationQuery
{
    public Task<IReadOnlyList<MarketListing>> ListByStatusAsync(
        ListingStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
