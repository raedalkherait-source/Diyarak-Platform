using Diyarak.Market.Application.Contractors;
using Diyarak.Market.Company;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlContractorRepository(PlatformDbContext context)
    : IContractorRepository
{
    public async Task<Contractor?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await context.MarketContractors
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Contractor?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var record = await context.MarketContractors
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<Contractor>> ListPublishedAsync(
        int offset,
        int take,
        string? governorate,
        ContractorSpecialty? specialty,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MarketContractorRecord> query = context.MarketContractors
            .AsNoTracking()
            .Where(r => r.IsPublished);

        if (governorate is not null)
            query = query.Where(r => r.Governorate == governorate);

        if (specialty is not null)
            query = query.Where(r => r.Specialty == (int)specialty);

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(offset)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default)
    {
        context.MarketContractors.Add(FromDomain(contractor));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveAsync(Contractor contractor, long expectedVersion, CancellationToken cancellationToken = default)
    {
        var record = FromDomain(contractor);
        record.Version = expectedVersion + 1;

        context.MarketContractors.Attach(record);
        context.Entry(record).State = EntityState.Modified;
        context.Entry(record).Property(r => r.Version).OriginalValue = expectedVersion;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await context.MarketContractors
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static MarketContractorRecord FromDomain(Contractor c) => new()
    {
        Id = c.Id,
        OwnerUserId = c.OwnerUserId,
        Version = c.Version,
        Name = c.Name,
        Description = c.Description,
        Governorate = c.Governorate,
        District = c.District,
        Specialty = (int)c.Specialty,
        Phone = c.Phone,
        IsPublished = c.IsPublished,
        CreatedAt = c.CreatedAt,
    };

    private static Contractor ToDomain(MarketContractorRecord r) =>
        Contractor.Restore(
            r.Id, r.OwnerUserId, r.Version,
            r.Name, r.Description,
            r.Governorate, r.District,
            (ContractorSpecialty)r.Specialty,
            r.Phone, r.IsPublished, r.CreatedAt);
}
