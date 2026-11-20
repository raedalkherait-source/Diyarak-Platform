using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class GetPublishedListingUseCase
{
    private readonly IMarketListingRepository _listingRepository;
    private readonly IMarketPropertyRepository _propertyRepository;
    private readonly IListingPhotoRepository? _photoRepository;

    public GetPublishedListingUseCase(
        IMarketListingRepository listingRepository,
        IMarketPropertyRepository propertyRepository,
        IListingPhotoRepository? photoRepository = null)
    {
        _listingRepository = listingRepository;
        _propertyRepository = propertyRepository;
        _photoRepository = photoRepository;
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

        IReadOnlyList<ListingPhoto> photos =
            _photoRepository is null
                ? []
                : await _photoRepository.ListByListingAsync(listing.Id, cancellationToken);

        return Result.Success(
            new PublishedListingProjection(listing, property)
            {
                Photos = photos,
            });
    }
}
