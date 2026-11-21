using Diyarak.Market.Company;
using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Agencies;

public interface IAgencyRepository
{
    public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken cancellationToken = default);

    public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>Agencies of the given publishers, keyed by owner (for listing pages).</summary>
    public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> when the owner already has an agency.</returns>
    public Task<bool> TryAddAsync(Agency agency, CancellationToken cancellationToken = default);

    public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken cancellationToken = default);
}

public static class AgencyErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.agency.invalid_request", "The agency details are invalid.");

    public static Error NotFound { get; } =
        Error.NotFound("market.agency.not_found", "The agency does not exist.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict("market.agency.concurrent_modification", "The agency changed meanwhile; reload and try again.");

    public static Error InvalidTransition { get; } =
        Error.Conflict("market.agency.invalid_transition", "This verification step is not possible in the agency's current state.");
}

public sealed record SaveAgencyCommand(string Name, string? Description, string? Governorate, string City, long? ExpectedVersion);

/// <summary>Creates the actor's agency, or updates it when <see cref="SaveAgencyCommand.ExpectedVersion"/> is given.</summary>
public sealed class SaveMyAgencyUseCase(IAgencyRepository agencies, IMarketTransactionRunner transactionRunner)
{
    public async Task<Result<Agency>> ExecuteAsync(Guid actorUserId, SaveAgencyCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (actorUserId == Guid.Empty ||
            (command.Governorate is { Length: > 0 } governorate && !Enum.TryParse<SyrianGovernorate>(governorate, ignoreCase: false, out _)) ||
            (command.Governorate is { Length: > 0 } && int.TryParse(command.Governorate, out _)))
        {
            return Result.Failure<Agency>(AgencyErrors.InvalidRequest);
        }

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                Agency? existing = await agencies.FindByOwnerAsync(actorUserId, token);

                try
                {
                    if (existing is null)
                    {
                        if (command.ExpectedVersion is not null)
                            return Result.Failure<Agency>(AgencyErrors.NotFound);

                        Agency created = Agency.Create(Guid.NewGuid(), actorUserId, command.Name, command.Description, command.Governorate, command.City);
                        return await agencies.TryAddAsync(created, token)
                            ? Result.Success(created)
                            : Result.Failure<Agency>(AgencyErrors.ConcurrentModification);
                    }

                    if (command.ExpectedVersion != existing.Version)
                        return Result.Failure<Agency>(AgencyErrors.ConcurrentModification);

                    existing.UpdateProfile(command.Name, command.Description, command.Governorate, command.City);
                }
                catch (ArgumentException)
                {
                    return Result.Failure<Agency>(AgencyErrors.InvalidRequest);
                }

                return await agencies.TrySaveAsync(existing, existing.Version, token)
                    ? Result.Success(existing)
                    : Result.Failure<Agency>(AgencyErrors.ConcurrentModification);
            },
            cancellationToken);
    }
}

public sealed class GetMyAgencyUseCase(IAgencyRepository agencies)
{
    public async Task<Result<Agency>> ExecuteAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure<Agency>(AgencyErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        return agency is null ? Result.Failure<Agency>(AgencyErrors.NotFound) : Result.Success(agency);
    }
}

public sealed class RequestAgencyVerificationUseCase(IAgencyRepository agencies)
{
    public async Task<Result<Agency>> ExecuteAsync(Guid actorUserId, string registrationNumber, long expectedVersion, CancellationToken cancellationToken = default)
    {
        Agency? agency = actorUserId == Guid.Empty ? null : await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<Agency>(AgencyErrors.NotFound);

        if (agency.Version != expectedVersion)
            return Result.Failure<Agency>(AgencyErrors.ConcurrentModification);

        try
        {
            agency.RequestVerification(registrationNumber);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Agency>(AgencyErrors.InvalidRequest);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<Agency>(AgencyErrors.InvalidTransition);
        }

        return await agencies.TrySaveAsync(agency, expectedVersion, cancellationToken)
            ? Result.Success(agency)
            : Result.Failure<Agency>(AgencyErrors.ConcurrentModification);
    }
}

public enum AgencyVerificationDecision
{
    Verify = 1,
    Reject = 2,
}

/// <summary>Moderators verify or reject a pending agency (authorization is enforced by the host).</summary>
public sealed class DecideAgencyVerificationUseCase(IAgencyRepository agencies, IClock clock)
{
    public async Task<Result<Agency>> ExecuteAsync(Guid agencyId, AgencyVerificationDecision decision, string? note, CancellationToken cancellationToken = default)
    {
        Agency? agency = agencyId == Guid.Empty ? null : await agencies.FindByIdAsync(agencyId, cancellationToken);
        if (agency is null)
            return Result.Failure<Agency>(AgencyErrors.NotFound);

        long version = agency.Version;

        try
        {
            if (decision == AgencyVerificationDecision.Verify)
                agency.Verify(clock.UtcNow);
            else if (decision == AgencyVerificationDecision.Reject)
                agency.Reject(note ?? string.Empty);
            else
                return Result.Failure<Agency>(AgencyErrors.InvalidRequest);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Agency>(AgencyErrors.InvalidRequest);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<Agency>(AgencyErrors.InvalidTransition);
        }

        return await agencies.TrySaveAsync(agency, version, cancellationToken)
            ? Result.Success(agency)
            : Result.Failure<Agency>(AgencyErrors.ConcurrentModification);
    }
}

public sealed record PublicAgencyPage(Agency Agency, IReadOnlyList<PublishedListingProjection> Listings, bool HasMore);

/// <summary>An agency's public page with its published listings, newest first.</summary>
public sealed class GetPublicAgencyUseCase(IAgencyRepository agencies, IPublishedListingQuery listings, Photos.IListingPhotoRepository photos)
{
    public const int PageSize = 24;

    public async Task<Result<PublicAgencyPage>> ExecuteAsync(Guid agencyId, int page, CancellationToken cancellationToken = default)
    {
        if (agencyId == Guid.Empty || page is < 1 or > 100)
            return Result.Failure<PublicAgencyPage>(AgencyErrors.InvalidRequest);

        Agency? agency = await agencies.FindByIdAsync(agencyId, cancellationToken);
        if (agency is null)
            return Result.Failure<PublicAgencyPage>(AgencyErrors.NotFound);

        IReadOnlyList<PublishedListingProjection> found = await listings.ListPageAsync(
            (page - 1) * PageSize,
            PageSize + 1,
            new PublishedListingSearchCriteria { PublisherUserId = agency.OwnerUserId },
            cancellationToken);

        PublishedListingProjection[] items = [.. found.Take(PageSize)];
        if (items.Length > 0)
        {
            IReadOnlyDictionary<Guid, IReadOnlyList<Diyarak.Market.Listing.ListingPhoto>> byListing =
                await photos.ListByListingsAsync([.. items.Select(item => item.Listing.Id)], cancellationToken);
            items = [.. items.Select(item => byListing.TryGetValue(item.Listing.Id, out var listingPhotos) ? item with { Photos = listingPhotos } : item)];
        }

        return Result.Success(new PublicAgencyPage(agency, items, found.Count > PageSize));
    }
}
