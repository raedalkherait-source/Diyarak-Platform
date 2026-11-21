using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;

namespace Diyarak.Api.Tests;

/// <summary>Agency repository with optional seeded agencies and no database access.</summary>
internal sealed class EmptyAgencyRepository : IAgencyRepository
{
    public List<Agency> Agencies { get; } = [];

    public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Agencies.SingleOrDefault(agency => agency.Id == agencyId));

    public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Agencies.SingleOrDefault(agency => agency.OwnerUserId == ownerUserId));

    public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(
            Agencies.Where(agency => ownerUserIds.Contains(agency.OwnerUserId)).ToDictionary(agency => agency.OwnerUserId));

    public Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> agencyIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(
            Agencies.Where(agency => agencyIds.Contains(agency.Id)).ToDictionary(agency => agency.Id));

    public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Agency>>([.. Agencies.Where(agency => agency.VerificationStatus == status).Take(take)]);

    public Task<bool> TryAddAsync(Agency agency, CancellationToken cancellationToken = default)
    {
        if (Agencies.Any(existing => existing.OwnerUserId == agency.OwnerUserId))
            return Task.FromResult(false);

        Agencies.Add(agency);
        return Task.FromResult(true);
    }

    public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken cancellationToken = default)
    {
        int index = Agencies.FindIndex(existing => existing.Id == agency.Id);
        if (index < 0 || Agencies[index].Version != expectedVersion)
            return Task.FromResult(false);

        Agencies[index] = Agency.Restore(
            agency.Id, agency.OwnerUserId, expectedVersion + 1, agency.Name, agency.Description, agency.Governorate, agency.City,
            agency.VerificationStatus, agency.RegistrationNumber, agency.VerificationNote, agency.VerifiedAt);
        return Task.FromResult(true);
    }
}
