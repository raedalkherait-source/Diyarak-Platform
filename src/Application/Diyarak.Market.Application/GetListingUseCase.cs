using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class GetListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;

    public GetListingUseCase(
        IMarketListingRepository listingRepository)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        _listingRepository = listingRepository;
    }

    public async Task<Result<MarketListing>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
        {
            return Result.Failure<MarketListing>(
                GetListingErrors.InvalidIdentifier);
        }

        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<MarketListing>(
                GetListingErrors.InvalidActorIdentifier);
        }

        MarketListing? listing =
            await _listingRepository.FindByIdAsync(
                listingId,
                cancellationToken);

        return listing is null ||
            listing.PublisherUserId != actorUserId
            ? Result.Failure<MarketListing>(
                GetListingErrors.NotFound)
            : Result.Success(listing);
    }
}
