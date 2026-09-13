using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class ListOwnedListingsUseCase
{
    private readonly IMarketListingRepository _listingRepository;

    public ListOwnedListingsUseCase(
        IMarketListingRepository listingRepository)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        _listingRepository = listingRepository;
    }

    public async Task<Result<IReadOnlyList<MarketListing>>> ExecuteAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<MarketListing>>(
                ListOwnedListingsErrors.InvalidActorIdentifier);
        }

        IReadOnlyList<MarketListing> listings =
            await _listingRepository.FindByPublisherUserIdAsync(
                actorUserId,
                cancellationToken);

        return Result.Success(listings);
    }
}
