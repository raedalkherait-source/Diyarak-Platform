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

    public async Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(
        Guid publisherUserId,
        CancellationToken cancellationToken = default)
    {
        List<MarketListingRecord> records =
            await _context.MarketListings
                .AsNoTracking()
                .Where(
                    listing =>
                        listing.PublisherUserId == publisherUserId)
                .ToListAsync(cancellationToken);

        return records
            .Select(MarketListingRecordMapper.ToDomain)
            .ToArray();
    }

    public async Task AddAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        MarketListingRecord record =
            MarketListingRecordMapper.FromDomain(listing);

        _context.MarketListings.Add(record);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveAsync(
        MarketListing listing,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);

        MarketListingRecord record =
            MarketListingRecordMapper.FromDomain(listing);

        record.Version = checked(expectedVersion + 1);

        var entry = _context.MarketListings.Attach(record);
        entry.State = EntityState.Modified;
        entry.Property(candidate => candidate.Version).OriginalValue =
            expectedVersion;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }
}
