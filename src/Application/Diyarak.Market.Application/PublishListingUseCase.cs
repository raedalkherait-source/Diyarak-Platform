using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class PublishListingUseCase
{
    private readonly IPropertyExistenceChecker _propertyExistenceChecker;

    public PublishListingUseCase(
        IPropertyExistenceChecker propertyExistenceChecker)
    {
        ArgumentNullException.ThrowIfNull(propertyExistenceChecker);

        _propertyExistenceChecker = propertyExistenceChecker;
    }

    public async Task ExecuteAsync(
        MarketListing listing,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);

        bool propertyExists = await _propertyExistenceChecker.ExistsAsync(
            listing.SubjectReference.SubjectId,
            cancellationToken);

        if (!propertyExists)
            throw new InvalidOperationException(
                "The referenced Property does not exist.");

        listing.Publish();
    }
}
