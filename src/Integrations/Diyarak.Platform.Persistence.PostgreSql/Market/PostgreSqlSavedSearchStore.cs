using Diyarak.Market.Application.SavedSearches;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlSavedSearchStore
    : ISavedSearchStore
{
    private readonly PlatformDbContext _context;

    public PostgreSqlSavedSearchStore(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);

        var record = new MarketSavedSearchRecord
        {
            Id = search.Id,
            UserId = search.UserId,
            Name = search.Name,
            Query = search.Query,
            CreatedAt = search.CreatedAt,
            LastSeenAt = search.LastSeenAt,
            AlertsEnabled = search.AlertsEnabled,
            LastAlertedAt = search.LastAlertedAt,
        };

        _context.MarketSavedSearches.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _context.Entry(record).State = EntityState.Detached;
        }
    }

    public async Task<IReadOnlyList<SavedSearch>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<MarketSavedSearchRecord> records = await _context.MarketSavedSearches
            .AsNoTracking()
            .Where(search => search.UserId == userId)
            .OrderByDescending(search => search.CreatedAt)
            .ThenBy(search => search.Id)
            .ToListAsync(cancellationToken);

        return [.. records.Select(record => new SavedSearch(
            record.Id, record.UserId, record.Name, record.Query, record.CreatedAt, record.LastSeenAt, record.AlertsEnabled, record.LastAlertedAt))];
    }

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.MarketSavedSearches.CountAsync(search => search.UserId == userId, cancellationToken);

    public Task<bool> ExistsWithQueryAsync(Guid userId, string query, CancellationToken cancellationToken = default) =>
        _context.MarketSavedSearches.AnyAsync(search => search.UserId == userId && search.Query == query, cancellationToken);

    public async Task<bool> MarkSeenAsync(Guid userId, Guid searchId, DateTimeOffset seenAt, CancellationToken cancellationToken = default)
    {
        int updated = await _context.MarketSavedSearches
            .Where(search => search.Id == searchId && search.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(search => search.LastSeenAt, seenAt), cancellationToken);

        return updated == 1;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid searchId, CancellationToken cancellationToken = default)
    {
        int deleted = await _context.MarketSavedSearches
            .Where(search => search.Id == searchId && search.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 1;
    }

    public async Task<bool> SetAlertsAsync(Guid userId, Guid searchId, bool enabled, CancellationToken cancellationToken = default)
    {
        int updated = await _context.MarketSavedSearches
            .Where(search => search.Id == searchId && search.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(search => search.AlertsEnabled, enabled), cancellationToken);

        return updated == 1;
    }

    public async Task<IReadOnlyList<Guid>> ListUsersDueForAlertsAsync(DateTimeOffset alertedBefore, int take, CancellationToken cancellationToken = default)
    {
        return await _context.MarketSavedSearches
            .AsNoTracking()
            .Where(search => search.AlertsEnabled)
            .GroupBy(search => search.UserId)
            .Where(group => group.Max(search => search.LastAlertedAt) == null || group.Max(search => search.LastAlertedAt) < alertedBefore)
            .OrderBy(group => group.Key)
            .Select(group => group.Key)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task MarkAlertedAsync(Guid userId, DateTimeOffset alertedAt, CancellationToken cancellationToken = default)
    {
        return _context.MarketSavedSearches
            .Where(search => search.UserId == userId && search.AlertsEnabled)
            .ExecuteUpdateAsync(setters => setters.SetProperty(search => search.LastAlertedAt, alertedAt), cancellationToken);
    }
}
