using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class UpdateListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IMarketTransactionRunner _transactionRunner;

    public UpdateListingUseCase(
        IMarketListingRepository listingRepository,
        IMarketTransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(transactionRunner);

        _listingRepository = listingRepository;
        _transactionRunner = transactionRunner;
    }

    public async Task<Result<long>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        UpdateListingPatch patch,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
            return Result.Failure<long>(UpdateListingErrors.InvalidIdentifier);

        if (actorUserId == Guid.Empty)
            return Result.Failure<long>(UpdateListingErrors.InvalidActorIdentifier);

        ArgumentNullException.ThrowIfNull(patch);

        if (patch.ExpectedVersion <= 0 ||
            (patch.UpdateContext && patch.Context is null) ||
            (patch.UpdateHeadline && patch.Headline is null) ||
            (patch.UpdatePrice && patch.Price is null))
        {
            return Result.Failure<long>(UpdateListingErrors.InvalidPatch);
        }

        if (!patch.HasChanges)
            return Result.Failure<long>(UpdateListingErrors.NoChanges);

        return await _transactionRunner.ExecuteAsync(
            async transactionalCancellationToken =>
            {
                MarketListing? listing =
                    await _listingRepository.FindByIdAsync(
                        listingId,
                        transactionalCancellationToken);

                if (listing is null ||
                    listing.PublisherUserId != actorUserId)
                {
                    return Result.Failure<long>(
                        UpdateListingErrors.NotFound);
                }

                if (listing.Version != patch.ExpectedVersion)
                {
                    return Result.Failure<long>(
                        UpdateListingErrors.ConcurrentModification);
                }

                if (listing.Status != ListingStatus.Draft)
                {
                    return Result.Failure<long>(
                        UpdateListingErrors.CannotEdit);
                }

                if (patch.UpdateContext)
                    listing.SetContext(patch.Context!);

                if (patch.UpdateHeadline)
                    listing.SetHeadline(patch.Headline!);

                if (patch.UpdatePrice)
                    listing.SetPrice(patch.Price!);

                if (patch.UpdateAvailableFromDate)
                {
                    if (patch.AvailableFromDate is null)
                        listing.ClearAvailableFromDate();
                    else
                        listing.SetAvailableFromDate(patch.AvailableFromDate);
                }

                bool saved =
                    await _listingRepository.TrySaveAsync(
                        listing,
                        patch.ExpectedVersion,
                        transactionalCancellationToken);

                if (!saved)
                {
                    return Result.Failure<long>(
                        UpdateListingErrors.ConcurrentModification);
                }

                return Result.Success(checked(patch.ExpectedVersion + 1));
            },
            cancellationToken);
    }
}
