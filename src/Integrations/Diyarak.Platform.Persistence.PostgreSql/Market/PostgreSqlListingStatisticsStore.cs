using Diyarak.Market.Application.Statistics;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlListingStatisticsStore
    : IListingStatisticsStore
{
    private readonly PlatformDbContext _context;

    public PostgreSqlListingStatisticsStore(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task IncrementAsync(Guid listingId, DateOnly day, ListingStatistic statistic, CancellationToken cancellationToken = default)
    {
        int views = statistic == ListingStatistic.View ? 1 : 0;
        int reveals = statistic == ListingStatistic.ContactReveal ? 1 : 0;

        // One statement, so concurrent increments of the same day never lose a count.
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO market.listing_daily_statistics (listing_id, day, views, contact_reveals)
            VALUES ({listingId}, {day}, {views}, {reveals})
            ON CONFLICT (listing_id, day) DO UPDATE
            SET views = market.listing_daily_statistics.views + EXCLUDED.views,
                contact_reveals = market.listing_daily_statistics.contact_reveals + EXCLUDED.contact_reveals
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ListingDailyStatistics>> ListDailyAsync(Guid listingId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        List<MarketListingDailyStatisticsRecord> records = await _context.MarketListingDailyStatistics
            .AsNoTracking()
            .Where(row => row.ListingId == listingId && row.Day >= from && row.Day <= to)
            .OrderBy(row => row.Day)
            .ToListAsync(cancellationToken);

        return [.. records.Select(row => new ListingDailyStatistics(row.Day, row.Views, row.ContactReveals))];
    }

    public async Task<IReadOnlyList<ListingStatisticsTotals>> SumAsync(IReadOnlyCollection<Guid> listingIds, DateOnly from, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listingIds);
        Guid[] ids = [.. listingIds];

        var rows = await _context.MarketListingDailyStatistics
            .AsNoTracking()
            .Where(row => ids.Contains(row.ListingId) && row.Day >= from)
            .GroupBy(row => row.ListingId)
            .Select(group => new { ListingId = group.Key, Views = group.Sum(row => row.Views), Reveals = group.Sum(row => row.ContactReveals) })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new ListingStatisticsTotals(row.ListingId, row.Views, row.Reveals))];
    }
}
