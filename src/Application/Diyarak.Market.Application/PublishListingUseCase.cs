using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class PublishListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;

    public PublishListingUseCase(
        IMarketListingRepository listingRepository,
        IPropertyExistenceChecker propertyExistenceChecker)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        ArgumentNullException.ThrowIfNull(propertyExistenceChecker);

        _listingRepository = listingRepository;
        _propertyExistenceChecker = propertyExistenceChecker;
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

        MarketListing? listing =
            await _listingRepository.FindByIdAsync(
                listingId,
                cancellationToken);

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
                cancellationToken);

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
            cancellationToken);

        return Result.Success();
    }
}
