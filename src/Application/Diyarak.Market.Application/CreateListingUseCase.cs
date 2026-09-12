using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class CreateListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;

    public CreateListingUseCase(
        IMarketListingRepository listingRepository,
        IPropertyExistenceChecker propertyExistenceChecker)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(propertyExistenceChecker);

        _listingRepository = listingRepository;
        _propertyExistenceChecker = propertyExistenceChecker;
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

        bool propertyExists =
            await _propertyExistenceChecker.ExistsAsync(
                propertyId,
                cancellationToken);

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
            cancellationToken);

        return Result.Success(listing.Id);
    }
}
