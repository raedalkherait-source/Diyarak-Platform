using Diyarak.Market.Application.Moderation;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlListingReportRepository
    : IListingReportRepository, IListingModerationQuery
{
    private readonly PlatformDbContext _context;

    public PostgreSqlListingReportRepository(PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddAsync(ListingReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        _context.MarketListingReports.Add(ToRecord(report));
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ListingReport>> ListOpenAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        List<MarketListingReportRecord> records = await _context.MarketListingReports
            .AsNoTracking()
            .Where(report => report.Resolution == null)
            .OrderBy(report => report.CreatedAt)
            .ThenBy(report => report.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<IReadOnlyList<ListingReport>> ListOpenByListingAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        List<MarketListingReportRecord> records = await _context.MarketListingReports
            .AsNoTracking()
            .Where(report => report.ListingId == listingId && report.Resolution == null)
            .OrderBy(report => report.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public Task<int> CountRecentAsync(Guid listingId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        _context.MarketListingReports
            .AsNoTracking()
            .CountAsync(report => report.ListingId == listingId && report.CreatedAt >= since, cancellationToken);

    public async Task SaveResolutionsAsync(IReadOnlyCollection<ListingReport> reports, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);

        foreach (ListingReport report in reports)
        {
            MarketListingReportRecord record = ToRecord(report);
            var entry = _context.MarketListingReports.Attach(record);
            entry.Property(candidate => candidate.Resolution).IsModified = true;
            entry.Property(candidate => candidate.ResolvedAt).IsModified = true;
            entry.Property(candidate => candidate.ResolvedByUserId).IsModified = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MarketListing>> ListByStatusAsync(
        ListingStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        List<MarketListingRecord> records = await _context.MarketListings
            .AsNoTracking()
            .Where(listing => listing.Status == (int)status)
            .OrderBy(listing => listing.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. records.Select(MarketListingRecordMapper.ToDomain)];
    }

    private static MarketListingReportRecord ToRecord(ListingReport report) =>
        new()
        {
            Id = report.Id,
            ListingId = report.ListingId,
            Reason = (int)report.Reason,
            Comment = report.Comment,
            CreatedAt = report.CreatedAt,
            Resolution = report.Resolution is { } resolution ? (int)resolution : null,
            ResolvedAt = report.ResolvedAt,
            ResolvedByUserId = report.ResolvedByUserId,
        };

    private static ListingReport ToDomain(MarketListingReportRecord record) =>
        ListingReport.Restore(
            record.Id,
            record.ListingId,
            (ListingReportReason)record.Reason,
            record.Comment,
            record.CreatedAt,
            record.Resolution is { } resolution ? (ListingReportResolution)resolution : null,
            record.ResolvedAt,
            record.ResolvedByUserId);
}
