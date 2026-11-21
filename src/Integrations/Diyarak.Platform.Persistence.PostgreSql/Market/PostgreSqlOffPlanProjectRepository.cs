using Diyarak.Market.Application.OffPlanProjects;
using Diyarak.Market.Company;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlOffPlanProjectRepository(PlatformDbContext context)
    : IOffPlanProjectRepository
{
    public async Task<OffPlanProject?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        MarketOffPlanProjectRecord? record = await context.MarketOffPlanProjects.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<OffPlanProject>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        List<MarketOffPlanProjectRecord> records = await context.MarketOffPlanProjects.AsNoTracking()
            .Where(p => p.AgencyId == agencyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<IReadOnlyList<OffPlanProject>> ListPublishedAsync(int offset, int take, string? governorate, CancellationToken cancellationToken = default)
    {
        IQueryable<MarketOffPlanProjectRecord> query = context.MarketOffPlanProjects.AsNoTracking()
            .Where(p => p.IsPublished);

        if (!string.IsNullOrEmpty(governorate))
            query = query.Where(p => p.Governorate == governorate);

        List<MarketOffPlanProjectRecord> records = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(offset)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task AddAsync(OffPlanProject project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        MarketOffPlanProjectRecord record = FromDomain(project);
        context.MarketOffPlanProjects.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        context.Entry(record).State = EntityState.Detached;
    }

    public async Task<bool> TrySaveAsync(OffPlanProject project, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        MarketOffPlanProjectRecord record = FromDomain(project);
        record.Version = checked(expectedVersion + 1);

        var entry = context.MarketOffPlanProjects.Attach(record);
        entry.State = EntityState.Modified;
        entry.Property(p => p.Version).OriginalValue = expectedVersion;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
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

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await context.MarketOffPlanProjects
            .Where(p => p.Id == projectId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static MarketOffPlanProjectRecord FromDomain(OffPlanProject project) => new()
    {
        Id = project.Id,
        AgencyId = project.AgencyId,
        Version = project.Version,
        Name = project.Name,
        Description = project.Description,
        Governorate = project.Governorate,
        District = project.District,
        ConstructionStatus = (int)project.ConstructionStatus,
        TotalUnits = project.TotalUnits,
        MinPriceUsd = project.MinPriceUsd,
        MaxPriceUsd = project.MaxPriceUsd,
        ExpectedCompletion = project.ExpectedCompletion,
        IsPublished = project.IsPublished,
        CreatedAt = project.CreatedAt,
    };

    private static OffPlanProject ToDomain(MarketOffPlanProjectRecord record) =>
        OffPlanProject.Restore(
            record.Id,
            record.AgencyId,
            record.Version,
            record.Name,
            record.Description,
            record.Governorate,
            record.District,
            (ConstructionStatus)record.ConstructionStatus,
            record.TotalUnits,
            record.MinPriceUsd,
            record.MaxPriceUsd,
            record.ExpectedCompletion,
            record.IsPublished,
            record.CreatedAt);
}
