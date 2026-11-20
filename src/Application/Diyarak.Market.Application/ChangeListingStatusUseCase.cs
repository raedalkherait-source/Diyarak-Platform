using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public enum ListingStatusChange
{
    Pause = 1,
    Resume = 2,
    Close = 3,
}

public static class ChangeListingStatusErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.listing_status.invalid_request",
            "The Listing identifier, actor and requested change must be valid; closing requires a reason.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing_status.not_found",
            "The Listing does not exist.");

    public static Error InvalidTransition { get; } =
        Error.Conflict(
            "market.listing_status.invalid_transition",
            "The Listing cannot make this status change from its current status.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.listing_status.concurrent_modification",
            "The Listing changed while the status change was being applied.");
}

/// <summary>Pauses, resumes or closes a Listing owned by the actor.</summary>
public sealed class ChangeListingStatusUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IMarketTransactionRunner _transactionRunner;

    public ChangeListingStatusUseCase(
        IMarketListingRepository listingRepository,
        IMarketTransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(transactionRunner);

        _listingRepository = listingRepository;
        _transactionRunner = transactionRunner;
    }

    /// <returns>The Listing's new version.</returns>
    public async Task<Result<long>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        ListingStatusChange change,
        ListingClosureReason? closureReason = null,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty ||
            actorUserId == Guid.Empty ||
            !Enum.IsDefined(change) ||
            (change == ListingStatusChange.Close) != closureReason.HasValue ||
            (closureReason is { } reason && !Enum.IsDefined(reason)))
        {
            return Result.Failure<long>(ChangeListingStatusErrors.InvalidRequest);
        }

        return await _transactionRunner.ExecuteAsync(
            async transactionalCancellationToken =>
            {
                MarketListing? listing =
                    await _listingRepository.FindByIdAsync(listingId, transactionalCancellationToken);

                // Missing and non-owned Listings are indistinguishable (ADR-0023).
                if (listing is null || listing.PublisherUserId != actorUserId)
                    return Result.Failure<long>(ChangeListingStatusErrors.NotFound);

                long expectedVersion = listing.Version;

                try
                {
                    switch (change)
                    {
                        case ListingStatusChange.Pause:
                            listing.Pause();
                            break;
                        case ListingStatusChange.Resume:
                            listing.Resume();
                            break;
                        case ListingStatusChange.Close:
                            listing.Close(closureReason!.Value);
                            break;
                    }
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure<long>(ChangeListingStatusErrors.InvalidTransition);
                }

                bool saved = await _listingRepository.TrySaveAsync(
                    listing,
                    expectedVersion,
                    transactionalCancellationToken);

                return saved
                    ? Result.Success(checked(expectedVersion + 1))
                    : Result.Failure<long>(ChangeListingStatusErrors.ConcurrentModification);
            },
            cancellationToken);
    }
}
