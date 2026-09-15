using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlPublishedListingQuery
    : IPublishedListingQuery
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPublishedListingQuery(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<IReadOnlyList<MarketListing>> ListPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        List<MarketListingRecord> records =
            await _context.MarketListings
                .AsNoTracking()
                .Where(
                    listing =>
                        listing.Status ==
                        (int)ListingStatus.Published)
                .OrderBy(listing => listing.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

        return records
            .Select(MarketListingRecordMapper.ToDomain)
            .ToArray();
    }
}
