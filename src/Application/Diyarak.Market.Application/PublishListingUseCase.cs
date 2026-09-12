using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class PublishListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;
    private readonly IMarketTransactionRunner _transactionRunner;

    public PublishListingUseCase(
        IMarketListingRepository listingRepository,
        IPropertyExistenceChecker propertyExistenceChecker,
        IMarketTransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(propertyExistenceChecker);
        ArgumentNullException.ThrowIfNull(transactionRunner);

        _listingRepository = listingRepository;
        _propertyExistenceChecker = propertyExistenceChecker;
        _transactionRunner = transactionRunner;
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

                try
                {
                    listing.Publish();
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure(
                        PublishListingErrors.CannotPublish);
                }

                await _listingRepository.SaveAsync(
                    listing,
                    transactionalCancellationToken);

                return Result.Success();
            },
            cancellationToken);
    }
}
