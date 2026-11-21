using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlAgencyRepository
    : IAgencyRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly PlatformDbContext _context;

    public PostgreSqlAgencyRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        MarketAgencyRecord? record = await _context.MarketAgencies.AsNoTracking()
            .SingleOrDefaultAsync(agency => agency.Id == agencyId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        MarketAgencyRecord? record = await _context.MarketAgencies.AsNoTracking()
            .SingleOrDefaultAsync(agency => agency.OwnerUserId == ownerUserId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownerUserIds);
        if (ownerUserIds.Count == 0)
            return new Dictionary<Guid, Agency>();

        Guid[] owners = [.. ownerUserIds.Distinct()];
        List<MarketAgencyRecord> records = await _context.MarketAgencies.AsNoTracking()
            .Where(agency => owners.Contains(agency.OwnerUserId))
            .ToListAsync(cancellationToken);

        return records.ToDictionary(record => record.OwnerUserId, ToDomain);
    }

    public async Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> agencyIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agencyIds);
        if (agencyIds.Count == 0)
            return new Dictionary<Guid, Agency>();

        Guid[] ids = [.. agencyIds.Distinct()];
        List<MarketAgencyRecord> records = await _context.MarketAgencies.AsNoTracking()
            .Where(agency => ids.Contains(agency.Id))
            .ToListAsync(cancellationToken);

        return records.ToDictionary(record => record.Id, ToDomain);
    }

    public async Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken cancellationToken = default)
    {
        List<MarketAgencyRecord> records = await _context.MarketAgencies.AsNoTracking()
            .Where(agency => agency.VerificationStatus == (int)status)
            .OrderBy(agency => agency.Name)
            .ThenBy(agency => agency.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<bool> TryAddAsync(Agency agency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agency);
        MarketAgencyRecord record = FromDomain(agency);
        _context.MarketAgencies.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException { SqlState: UniqueViolationSqlState })
        {
            return false;
        }
        finally
        {
            _context.Entry(record).State = EntityState.Detached;
        }
    }

    public async Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agency);
        MarketAgencyRecord record = FromDomain(agency);
        record.Version = checked(expectedVersion + 1);

        var entry = _context.MarketAgencies.Attach(record);
        entry.State = EntityState.Modified;
        entry.Property(candidate => candidate.Version).OriginalValue = expectedVersion;

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
            entry.State = EntityState.Detached;
        }
    }

    private static MarketAgencyRecord FromDomain(Agency agency) => new()
    {
        Id = agency.Id,
        OwnerUserId = agency.OwnerUserId,
        Version = agency.Version,
        Name = agency.Name,
        Description = agency.Description,
        Governorate = agency.Governorate,
        City = agency.City,
        VerificationStatus = (int)agency.VerificationStatus,
        RegistrationNumber = agency.RegistrationNumber,
        VerificationNote = agency.VerificationNote,
        VerifiedAt = agency.VerifiedAt,
    };

    private static Agency ToDomain(MarketAgencyRecord record) =>
        Agency.Restore(
            record.Id,
            record.OwnerUserId,
            record.Version,
            record.Name,
            record.Description,
            record.Governorate,
            record.City,
            (AgencyVerificationStatus)record.VerificationStatus,
            record.RegistrationNumber,
            record.VerificationNote,
            record.VerifiedAt);
}
