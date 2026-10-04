using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class GetPublishedListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IMarketPropertyRepository _propertyRepository;

    public GetPublishedListingUseCase(
        IMarketListingRepository listingRepository,
        IMarketPropertyRepository propertyRepository)
    {
        _listingRepository = listingRepository;
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<PublishedListingProjection>> ExecuteAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
        {
            return Result.Failure<PublishedListingProjection>(
                GetListingErrors.InvalidIdentifier);
        }

        MarketListing? listing =
            await _listingRepository.FindByIdAsync(
                listingId,
                cancellationToken);

        if (listing is null || listing.Status != ListingStatus.Published)
        {
            return Result.Failure<PublishedListingProjection>(
                GetListingErrors.NotFound);
        }

        if (listing.SubjectReference.SubjectType !=
            MarketListingSubjectTypes.Property)
        {
            return Result.Failure<PublishedListingProjection>(
                GetListingErrors.NotFound);
        }

        MarketProperty? property =
            await _propertyRepository.FindByIdAsync(
                listing.SubjectReference.SubjectId,
                cancellationToken);

        if (property is null)
        {
            return Result.Failure<PublishedListingProjection>(
                GetListingErrors.NotFound);
        }

        return Result.Success(
            new PublishedListingProjection(listing, property));
    }
}