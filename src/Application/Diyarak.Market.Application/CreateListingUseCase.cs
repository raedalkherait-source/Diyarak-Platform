using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class CreateListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyListingAuthorizationChecker
        _propertyListingAuthorizationChecker;
    private readonly IMarketTransactionRunner _transactionRunner;

    public CreateListingUseCase(
        IMarketListingRepository listingRepository,
        IPropertyListingAuthorizationChecker
            propertyListingAuthorizationChecker,
        IMarketTransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(
            propertyListingAuthorizationChecker);
        ArgumentNullException.ThrowIfNull(transactionRunner);

        _listingRepository = listingRepository;
        _propertyListingAuthorizationChecker =
            propertyListingAuthorizationChecker;
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
                bool canCreateListing =
                    await _propertyListingAuthorizationChecker
                        .CanCreateListingAsync(
                            propertyId,
                            actorUserId,
                            transactionalCancellationToken);

                if (!canCreateListing)
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
