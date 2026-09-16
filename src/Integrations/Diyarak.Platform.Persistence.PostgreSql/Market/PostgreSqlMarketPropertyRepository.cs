using Diyarak.Market.Application;
using Microsoft.EntityFrameworkCore;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlMarketPropertyRepository
    : IMarketPropertyRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlMarketPropertyRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<MarketProperty?> FindByIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        MarketPropertyRecord? record =
            await _context.MarketProperties
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    property => property.Id == propertyId,
                    cancellationToken);

        return record is null
            ? null
            : MarketPropertyRecordMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        List<MarketPropertyRecord> records =
            await _context.MarketProperties
                .AsNoTracking()
                .Where(property => property.OwnerUserId == ownerUserId)
                .ToListAsync(cancellationToken);

        return records
            .Select(MarketPropertyRecordMapper.ToDomain)
            .ToArray();
    }

    public async Task<IReadOnlyList<MarketProperty>> FindPageByOwnerUserIdAsync(
        Guid ownerUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        List<MarketPropertyRecord> records =
            await _context.MarketProperties
                .AsNoTracking()
                .Where(property => property.OwnerUserId == ownerUserId)
                .OrderBy(property => property.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

        return records
            .Select(MarketPropertyRecordMapper.ToDomain)
            .ToArray();
    }

    public async Task AddAsync(
        MarketProperty property,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(property);

        _context.MarketProperties.Add(
            MarketPropertyRecordMapper.FromDomain(property));

        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task<bool> TrySaveAsync(
        MarketProperty property,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);

        MarketPropertyRecord record =
            MarketPropertyRecordMapper.FromDomain(property);

        record.Version = checked(expectedVersion + 1);

        var entry = _context.MarketProperties.Attach(record);
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
