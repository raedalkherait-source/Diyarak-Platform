using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class CreateListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;
    private readonly IMarketTransactionRunner _transactionRunner;

    public CreateListingUseCase(
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

    public async Task<Result<Guid>> ExecuteAsync(
        Guid propertyId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (propertyId == Guid.Empty)
        {
            return Result.Failure<Guid>(
                CreateListingErrors.InvalidPropertyIdentifier);
        }

        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<Guid>(
                CreateListingErrors.InvalidActorIdentifier);
        }

        return await _transactionRunner.ExecuteAsync(
            async transactionalCancellationToken =>
            {
                bool propertyExists =
                    await _propertyExistenceChecker.ExistsAsync(
                        propertyId,
                        transactionalCancellationToken);

                if (!propertyExists)
                {
                    return Result.Failure<Guid>(
                        CreateListingErrors.PropertyNotFound);
                }

                var listing = new MarketListing(
                    Guid.NewGuid(),
                    actorUserId,
                    new ListingSubjectReference(
                        propertyId,
                        MarketListingSubjectTypes.Property));

                await _listingRepository.AddAsync(
                    listing,
                    transactionalCancellationToken);

                return Result.Success(listing.Id);
            },
            cancellationToken);
    }
}
