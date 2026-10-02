using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class ListPublishedListingsUseCase
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IPublishedListingQuery _publishedListingQuery;

    public ListPublishedListingsUseCase(
        IPublishedListingQuery publishedListingQuery)
    {
        ArgumentNullException.ThrowIfNull(publishedListingQuery);
        _publishedListingQuery = publishedListingQuery;
    }

    public async Task<Result<PublishedListingPage>> ExecuteAsync(
        int page,
        int pageSize,
        PublishedListingSearchCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0 ||
            pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidPagination);
        }

        if (criteria?.Price is { } price &&
            (price.Minimum < 0 ||
             price.Maximum < 0 ||
             price.Minimum > price.Maximum ||
             ((price.Minimum is not null ||
               price.Maximum is not null) &&
              price.Currency is null) ||
             (price.Minimum is null &&
              price.Maximum is null &&
              price.Currency is not null)))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.LivingArea is { } livingArea &&
            (livingArea.Minimum < 0 ||
             livingArea.Maximum < 0 ||
             livingArea.Minimum > livingArea.Maximum ||
             ((livingArea.Minimum is not null ||
               livingArea.Maximum is not null) &&
              livingArea.Unit is null) ||
             (livingArea.Minimum is null &&
              livingArea.Maximum is null &&
              livingArea.Unit is not null) ||
             (livingArea.Unit is { } unit &&
              !Enum.IsDefined(unit))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Rooms?.TotalRooms is { } totalRooms &&
            (totalRooms.Minimum < 0 ||
             totalRooms.Maximum < 0 ||
             totalRooms.Minimum > totalRooms.Maximum))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Rooms?.BedroomCount is { } bedroomCount &&
            (bedroomCount.Minimum < 0 ||
             bedroomCount.Maximum < 0 ||
             bedroomCount.Minimum > bedroomCount.Maximum))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Rooms?.BathroomCount is { } bathroomCount &&
            (bathroomCount.Minimum < 0 ||
             bathroomCount.Maximum < 0 ||
             bathroomCount.Minimum > bathroomCount.Maximum))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.ConstructionYear is { } constructionYear &&
            ((constructionYear.Minimum is { } minimumConstructionYear &&
              minimumConstructionYear < 1) ||
             (constructionYear.Maximum is { } maximumConstructionYear &&
              maximumConstructionYear < 1) ||
             constructionYear.Minimum > constructionYear.Maximum))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.ParkingSpaceCount is { } parkingSpaceCount &&
            (parkingSpaceCount.Minimum < 0 ||
             parkingSpaceCount.Maximum < 0 ||
             parkingSpaceCount.Minimum > parkingSpaceCount.Maximum))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.TransactionIntents is { } transactionIntents &&
            (transactionIntents.Count == 0 ||
             transactionIntents.Any(intent => !Enum.IsDefined(intent))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Categories is { } categories &&
            (categories.Count == 0 ||
             categories.Any(category => !Enum.IsDefined(category))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.CommercialSubtypes is { } commercialSubtypes &&
            (commercialSubtypes.Count == 0 ||
             commercialSubtypes.Any(subtype => !Enum.IsDefined(subtype))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.FurnishingQualities is { } furnishingQualities &&
            (furnishingQualities.Count == 0 ||
             furnishingQualities.Any(quality => !Enum.IsDefined(quality))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.RequiredFeatures is { } requiredFeatures &&
            (requiredFeatures.Count == 0 ||
             requiredFeatures.Any(feature => !Enum.IsDefined(feature))))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Location is { City: not null } location &&
            string.IsNullOrWhiteSpace(location.City))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        if (criteria?.Property?.Location is { PostalCode: not null } postalLocation &&
            string.IsNullOrWhiteSpace(postalLocation.PostalCode))
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidSearchCriteria);
        }
        long skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidPagination);
        }

        IReadOnlyList<MarketListing> candidates =
            await _publishedListingQuery.ListPageAsync(
                (int)skip,
                pageSize + 1,
                criteria,
                cancellationToken);

        if (candidates.Any(
                listing =>
                    listing.Status != ListingStatus.Published))
        {
            throw new InvalidOperationException(
                "The published Listing query returned a non-published Listing.");
        }

        bool hasMore = candidates.Count > pageSize;
        MarketListing[] items = candidates
            .Take(pageSize)
            .ToArray();

        return Result.Success(
            new PublishedListingPage(
                items,
                page,
                pageSize,
                hasMore));
    }
}
