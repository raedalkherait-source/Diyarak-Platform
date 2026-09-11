using Diyarak.Market.Application;
using Microsoft.EntityFrameworkCore;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlMarketListingRepository
    : IMarketListingRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlMarketListingRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public async Task<MarketListing?> FindByIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        MarketListingRecord? record =
            await _context.MarketListings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    listing => listing.Id == listingId,
                    cancellationToken);

        return record is null
            ? null
            : MarketListingRecordMapper.ToDomain(record);
    }

    public async Task SaveAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        MarketListingRecord record =
            MarketListingRecordMapper.FromDomain(listing);

        _context.MarketListings.Update(record);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
