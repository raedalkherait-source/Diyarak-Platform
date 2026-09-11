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

    public async Task ExecuteAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
            throw new ArgumentException(
                "Listing identifier cannot be empty.",
                nameof(listingId));

        MarketListing? listing =
            await _listingRepository.FindByIdAsync(
                listingId,
                cancellationToken);

        if (listing is null)
            throw new InvalidOperationException(
                "The Listing does not exist.");

        bool propertyExists =
            await _propertyExistenceChecker.ExistsAsync(
                listing.SubjectReference.SubjectId,
                cancellationToken);

        if (!propertyExists)
            throw new InvalidOperationException(
                "The referenced Property does not exist.");

        listing.Publish();

        await _listingRepository.SaveAsync(
            listing,
            cancellationToken);
    }
}
