using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class PublishListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;
    private readonly IMarketTransactionRunner _transactionRunner;
    private readonly IActiveListingCounter _activeListingCounter;
    private readonly IAgencyRepository _agencies;
    private readonly IAgencyPackageRepository _packages;
    private readonly IClock _clock;

    public PublishListingUseCase(
        IMarketListingRepository listingRepository,
        IPropertyExistenceChecker propertyExistenceChecker,
        IMarketTransactionRunner transactionRunner,
        IActiveListingCounter activeListingCounter,
        IAgencyRepository agencies,
        IAgencyPackageRepository packages,
        IClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(propertyExistenceChecker);
        ArgumentNullException.ThrowIfNull(transactionRunner);
        ArgumentNullException.ThrowIfNull(activeListingCounter);
        ArgumentNullException.ThrowIfNull(agencies);
        ArgumentNullException.ThrowIfNull(packages);

        _listingRepository = listingRepository;
        _propertyExistenceChecker = propertyExistenceChecker;
        _transactionRunner = transactionRunner;
        _activeListingCounter = activeListingCounter;
        _agencies = agencies;
        _packages = packages;
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
        {
            return Result.Failure(
                PublishListingErrors.InvalidIdentifier);
        }

        if (actorUserId == Guid.Empty)
        {
            return Result.Failure(
                PublishListingErrors.InvalidActorIdentifier);
        }

        return await _transactionRunner.ExecuteAsync(
            async transactionalCancellationToken =>
            {
                MarketListing? listing =
                    await _listingRepository.FindByIdAsync(
                        listingId,
                        transactionalCancellationToken);

                if (listing is null)
                {
                    return Result.Failure(
                        PublishListingErrors.NotFound);
                }

                if (listing.PublisherUserId != actorUserId)
                {
                    return Result.Failure(
                        PublishListingErrors.NotFound);
                }

                bool propertyExists =
                    await _propertyExistenceChecker.ExistsAsync(
                        listing.SubjectReference.SubjectId,
                        transactionalCancellationToken);

                if (!propertyExists)
                {
                    return Result.Failure(
                        PublishListingErrors.PropertyNotFound);
                }

                int activeCount = await _activeListingCounter
                    .CountActiveByPublisherAsync(actorUserId, transactionalCancellationToken);

                int quota = await ResolveQuotaAsync(actorUserId, transactionalCancellationToken);

                if (activeCount >= quota)
                {
                    return Result.Failure(
                        PublishListingErrors.QuotaExceeded);
                }

                long expectedVersion = listing.Version;

                try
                {
                    listing.Publish(_clock.UtcNow);
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure(
                        PublishListingErrors.CannotPublish);
                }

                bool saved =
                    await _listingRepository.TrySaveAsync(
                        listing,
                        expectedVersion,
                        transactionalCancellationToken);

                if (!saved)
                {
                    return Result.Failure(
                        PublishListingErrors.ConcurrentModification);
                }

                return Result.Success();
            },
            cancellationToken);
    }

    private async Task<int> ResolveQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        Agency? agency = await _agencies.FindByOwnerAsync(userId, cancellationToken);
        if (agency is null)
            return AgencyPackage.DefaultFreeQuota;

        AgencyPackage? activePackage = await _packages
            .FindActiveByAgencyAsync(agency.Id, _clock.UtcNow, cancellationToken);

        return activePackage?.ListingQuota ?? AgencyPackage.DefaultFreeQuota;
    }
}
