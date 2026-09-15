using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class GetPublishedListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;

    public GetPublishedListingUseCase(
        IMarketListingRepository listingRepository)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        _listingRepository = listingRepository;
    }

    public async Task<Result<MarketListing>> ExecuteAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
        {
            return Result.Failure<MarketListing>(
                GetListingErrors.InvalidIdentifier);
        }

        MarketListing? listing =
            await _listingRepository.FindByIdAsync(
                listingId,
                cancellationToken);

        return listing is null ||
            listing.Status != ListingStatus.Published
            ? Result.Failure<MarketListing>(
                GetListingErrors.NotFound)
            : Result.Success(listing);
    }
}
