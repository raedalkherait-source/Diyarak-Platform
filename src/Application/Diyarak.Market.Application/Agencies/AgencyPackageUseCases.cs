using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Agencies;

public interface IAgencyPackageRepository
{
    public Task<AgencyPackage?> FindByIdAsync(Guid packageId, CancellationToken cancellationToken = default);

    public Task<AgencyPackage?> FindActiveByAgencyAsync(Guid agencyId, DateTimeOffset now, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<AgencyPackage>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);

    public Task AddAsync(AgencyPackage package, CancellationToken cancellationToken = default);

    public Task<bool> TrySaveAsync(AgencyPackage package, CancellationToken cancellationToken = default);
}

/// <summary>Counts Published + Paused listings per publisher, used for quota enforcement.</summary>
public interface IActiveListingCounter
{
    public Task<int> CountActiveByPublisherAsync(Guid publisherUserId, CancellationToken cancellationToken = default);
}

public static class AgencyPackageErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.agency_package.invalid_request", "The package details are invalid.");

    public static Error AgencyNotFound { get; } =
        Error.NotFound("market.agency_package.agency_not_found", "The agency does not exist.");

    public static Error PackageNotFound { get; } =
        Error.NotFound("market.agency_package.not_found", "The package does not exist.");

    public static Error AlreadyRevoked { get; } =
        Error.Conflict("market.agency_package.already_revoked", "The package is already revoked.");
}

/// <summary>Moderators assign a listing-quota package to an agency.</summary>
public sealed class AssignAgencyPackageUseCase(
    IAgencyRepository agencies,
    IAgencyPackageRepository packages,
    IClock clock)
{
    public async Task<Result<AgencyPackage>> ExecuteAsync(
        Guid agencyId, int listingQuota, int durationDays, string? note,
        CancellationToken cancellationToken = default)
    {
        if (agencyId == Guid.Empty || listingQuota <= 0 || durationDays <= 0 || durationDays > 365)
            return Result.Failure<AgencyPackage>(AgencyPackageErrors.InvalidRequest);

        Agency? agency = await agencies.FindByIdAsync(agencyId, cancellationToken);
        if (agency is null)
            return Result.Failure<AgencyPackage>(AgencyPackageErrors.AgencyNotFound);

        DateTimeOffset now = clock.UtcNow;
        AgencyPackage package;

        try
        {
            package = AgencyPackage.Create(
                Guid.NewGuid(), agencyId, listingQuota,
                now.AddDays(durationDays), note, now);
        }
        catch (ArgumentException)
        {
            return Result.Failure<AgencyPackage>(AgencyPackageErrors.InvalidRequest);
        }

        await packages.AddAsync(package, cancellationToken);
        return Result.Success(package);
    }
}

/// <summary>Moderators revoke a package early.</summary>
public sealed class RevokeAgencyPackageUseCase(IAgencyPackageRepository packages, IClock clock)
{
    public async Task<Result> ExecuteAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        if (packageId == Guid.Empty)
            return Result.Failure(AgencyPackageErrors.InvalidRequest);

        AgencyPackage? package = await packages.FindByIdAsync(packageId, cancellationToken);
        if (package is null)
            return Result.Failure(AgencyPackageErrors.PackageNotFound);

        try
        {
            package.Revoke(clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(AgencyPackageErrors.AlreadyRevoked);
        }

        return await packages.TrySaveAsync(package, cancellationToken)
            ? Result.Success()
            : Result.Failure(AgencyPackageErrors.PackageNotFound);
    }
}

/// <summary>Returns the user's effective listing quota: active package quota, or the free default.</summary>
public sealed class GetListingQuotaUseCase(
    IAgencyRepository agencies,
    IAgencyPackageRepository packages,
    IActiveListingCounter counter,
    IClock clock)
{
    public async Task<ListingQuotaInfo> ExecuteAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        int activeCount = await counter.CountActiveByPublisherAsync(actorUserId, cancellationToken);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return new ListingQuotaInfo(AgencyPackage.DefaultFreeQuota, activeCount, null);

        AgencyPackage? activePackage = await packages.FindActiveByAgencyAsync(agency.Id, clock.UtcNow, cancellationToken);
        int quota = activePackage?.ListingQuota ?? AgencyPackage.DefaultFreeQuota;

        return new ListingQuotaInfo(quota, activeCount, activePackage);
    }
}

public sealed record ListingQuotaInfo(int Quota, int ActiveListings, AgencyPackage? ActivePackage)
{
    public int Remaining => Math.Max(0, Quota - ActiveListings);
    public bool CanPublish => ActiveListings < Quota;
}
