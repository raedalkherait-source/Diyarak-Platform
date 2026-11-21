using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlAgencyPackageRepository : IAgencyPackageRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlAgencyPackageRepository(PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<AgencyPackage?> FindByIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        MarketAgencyPackageRecord? record = await _context.MarketAgencyPackages.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == packageId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<AgencyPackage?> FindActiveByAgencyAsync(Guid agencyId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        MarketAgencyPackageRecord? record = await _context.MarketAgencyPackages.AsNoTracking()
            .Where(p => p.AgencyId == agencyId && !p.IsRevoked && p.ExpiresAt > now)
            .OrderByDescending(p => p.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<AgencyPackage>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        List<MarketAgencyPackageRecord> records = await _context.MarketAgencyPackages.AsNoTracking()
            .Where(p => p.AgencyId == agencyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    public async Task AddAsync(AgencyPackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        _context.MarketAgencyPackages.Add(FromDomain(package));
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveAsync(AgencyPackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        MarketAgencyPackageRecord record = FromDomain(package);
        _context.MarketAgencyPackages.Update(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        finally
        {
            _context.Entry(record).State = EntityState.Detached;
        }
    }

    private static MarketAgencyPackageRecord FromDomain(AgencyPackage package) => new()
    {
        Id = package.Id,
        AgencyId = package.AgencyId,
        ListingQuota = package.ListingQuota,
        ExpiresAt = package.ExpiresAt,
        Note = package.Note,
        CreatedAt = package.CreatedAt,
        IsRevoked = package.IsRevoked,
        RevokedAt = package.RevokedAt,
    };

    private static AgencyPackage ToDomain(MarketAgencyPackageRecord record) =>
        AgencyPackage.Restore(
            record.Id, record.AgencyId, record.ListingQuota,
            record.ExpiresAt, record.Note, record.CreatedAt,
            record.IsRevoked, record.RevokedAt);
}
