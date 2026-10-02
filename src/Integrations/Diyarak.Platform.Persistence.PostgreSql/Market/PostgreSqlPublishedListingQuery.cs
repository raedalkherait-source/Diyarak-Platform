using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlPublishedListingQuery
    : IPublishedListingQuery
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPublishedListingQuery(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<IReadOnlyList<MarketListing>> ListPageAsync(
        int skip,
        int take,
        PublishedListingSearchCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IQueryable<MarketListingRecord> query =
            BuildQuery(criteria);
        List<MarketListingRecord> records =
            await query
                .OrderBy(listing => listing.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

        return records
            .Select(MarketListingRecordMapper.ToDomain)
            .ToArray();
    }

    internal IQueryable<MarketListingRecord> BuildQuery(
        PublishedListingSearchCriteria? criteria)
    {
        IQueryable<MarketListingRecord> query =
            _context.MarketListings
                .AsNoTracking()
                .Where(
                    listing =>
                        listing.Status ==
                        (int)ListingStatus.Published);

        if (criteria?.TransactionIntents is { Count: > 0 } transactionIntents)
        {
            int[] intentValues =
                transactionIntents
                    .Select(intent => (int)intent)
                    .ToArray();

            query =
                query.Where(
                    listing =>
                        listing.TransactionIntent.HasValue &&
                        intentValues.Contains(listing.TransactionIntent.Value));
        }

        if (criteria?.Price is
            {
                Currency: not null,
            } price)
        {
            decimal? minimum = price.Minimum;
            decimal? maximum = price.Maximum;
            string currencyCode = price.Currency.Code;

            query =
                query.Where(
                    listing =>
                        listing.PriceIsOnRequest == false &&
                        listing.PriceAmount.HasValue &&
                        listing.PriceCurrency == currencyCode &&
                        (!minimum.HasValue ||
                         listing.PriceAmount.Value >= minimum.Value) &&
                        (!maximum.HasValue ||
                         listing.PriceAmount.Value <= maximum.Value));
        }

        if (criteria?.Property?.Categories is { Count: > 0 } categories)
        {
            int[] categoryValues =
                categories
                    .Select(category => (int)category)
                    .ToArray();

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                categoryValues.Contains(property.Category)));
        }

        if (criteria?.Property?.CommercialSubtypes is
            { Count: > 0 } commercialSubtypes)
        {
            int[] subtypeValues =
                commercialSubtypes
                    .Select(subtype => (int)subtype)
                    .ToArray();

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.Category ==
                                (int)Diyarak.Market.Property.PropertyCategory.CommercialProperty &&
                                property.CommercialSubtype.HasValue &&
                                subtypeValues.Contains(
                                    property.CommercialSubtype.Value)));
        }

        if (criteria?.Property?.Location is { City: not null } location)
        {
            string cityPattern =
                EscapeLikePattern(location.City);

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                EF.Functions.ILike(
                                    property.City,
                                    cityPattern,
                                    @"\")));
        }

        if (criteria?.Property?.Location is
            { PostalCode: not null } postalLocation)
        {
            string postalCodePattern =
                EscapeLikePattern(postalLocation.PostalCode);

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                EF.Functions.ILike(
                                    property.PostalCode,
                                    postalCodePattern,
                                    @"\")));
        }
        if (criteria?.Property?.LivingArea is
            { Unit: not null } livingArea)
        {
            decimal? minimumSquareMeters =
                livingArea.Minimum.HasValue
                    ? new Area(
                        livingArea.Minimum.Value,
                        livingArea.Unit.Value)
                        .ConvertTo(AreaUnit.SquareMeter)
                        .Value
                    : null;

            decimal? maximumSquareMeters =
                livingArea.Maximum.HasValue
                    ? new Area(
                        livingArea.Maximum.Value,
                        livingArea.Unit.Value)
                        .ConvertTo(AreaUnit.SquareMeter)
                        .Value
                    : null;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.LivingAreaValue.HasValue &&
                                property.LivingAreaUnit.HasValue &&
                                (
                                    property.LivingAreaUnit.Value ==
                                    (int)AreaUnit.SquareMeter
                                        ? property.LivingAreaValue.Value
                                        : property.LivingAreaUnit.Value ==
                                          (int)AreaUnit.SquareFoot
                                            ? property.LivingAreaValue.Value *
                                              0.09290304m
                                            : property.LivingAreaUnit.Value ==
                                              (int)AreaUnit.Hectare
                                                ? property.LivingAreaValue.Value *
                                                  10_000m
                                                : property.LivingAreaUnit.Value ==
                                                  (int)AreaUnit.Dunum
                                                    ? property.LivingAreaValue.Value *
                                                      1_000m
                                                    : (decimal?)null
                                ).HasValue &&
                                (!minimumSquareMeters.HasValue ||
                                 (
                                     property.LivingAreaUnit.Value ==
                                     (int)AreaUnit.SquareMeter
                                         ? property.LivingAreaValue.Value
                                         : property.LivingAreaUnit.Value ==
                                           (int)AreaUnit.SquareFoot
                                             ? property.LivingAreaValue.Value *
                                               0.09290304m
                                             : property.LivingAreaUnit.Value ==
                                               (int)AreaUnit.Hectare
                                                 ? property.LivingAreaValue.Value *
                                                   10_000m
                                                 : property.LivingAreaUnit.Value ==
                                                   (int)AreaUnit.Dunum
                                                     ? property.LivingAreaValue.Value *
                                                       1_000m
                                                     : (decimal?)null
                                 ) >= minimumSquareMeters) &&
                                (!maximumSquareMeters.HasValue ||
                                 (
                                     property.LivingAreaUnit.Value ==
                                     (int)AreaUnit.SquareMeter
                                         ? property.LivingAreaValue.Value
                                         : property.LivingAreaUnit.Value ==
                                           (int)AreaUnit.SquareFoot
                                             ? property.LivingAreaValue.Value *
                                               0.09290304m
                                             : property.LivingAreaUnit.Value ==
                                               (int)AreaUnit.Hectare
                                                 ? property.LivingAreaValue.Value *
                                                   10_000m
                                                 : property.LivingAreaUnit.Value ==
                                                   (int)AreaUnit.Dunum
                                                     ? property.LivingAreaValue.Value *
                                                       1_000m
                                                     : (decimal?)null
                                 ) <= maximumSquareMeters)));
        }
        if (criteria?.Property?.Rooms?.TotalRooms is { } totalRooms)
        {
            decimal? minimum = totalRooms.Minimum;
            decimal? maximum = totalRooms.Maximum;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.TotalRooms.HasValue &&
                                (!minimum.HasValue ||
                                 property.TotalRooms.Value >= minimum.Value) &&
                                (!maximum.HasValue ||
                                 property.TotalRooms.Value <= maximum.Value)));
        }
        if (criteria?.Property?.Rooms?.BedroomCount is { } bedroomCount)
        {
            int? minimum = bedroomCount.Minimum;
            int? maximum = bedroomCount.Maximum;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.BedroomCount.HasValue &&
                                (!minimum.HasValue ||
                                 property.BedroomCount.Value >= minimum.Value) &&
                                (!maximum.HasValue ||
                                 property.BedroomCount.Value <= maximum.Value)));
        }
        if (criteria?.Property?.Rooms?.BathroomCount is { } bathroomCount)
        {
            int? minimum = bathroomCount.Minimum;
            int? maximum = bathroomCount.Maximum;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.BathroomCount.HasValue &&
                                (!minimum.HasValue ||
                                 property.BathroomCount.Value >= minimum.Value) &&
                                (!maximum.HasValue ||
                                 property.BathroomCount.Value <= maximum.Value)));
        }
        if (criteria?.Property?.FurnishingQualities is { Count: > 0 } furnishingQualities)
        {
            int[] values =
                furnishingQualities
                    .Select(quality => (int)quality)
                    .Distinct()
                    .ToArray();

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.FurnishingQuality.HasValue &&
                                values.Contains(
                                    property.FurnishingQuality.Value)));
        }
        if (criteria?.Property?.RequiredFeatures is { Count: > 0 } requiredFeatures)
        {
            int[] requiredFeatureValues =
                requiredFeatures
                    .Select(feature => (int)feature)
                    .Distinct()
                    .ToArray();

            foreach (int requiredFeature in requiredFeatureValues)
            {
                int feature = requiredFeature;

                query =
                    query.Where(
                        listing =>
                            listing.SubjectType ==
                            MarketListingSubjectTypes.Property &&
                            _context.MarketProperties.Any(
                                property =>
                                    property.Id == listing.SubjectId &&
                                    property.Features.Contains(feature)));
            }
        }
        if (criteria?.Property?.ConstructionYear is { } constructionYear)
        {
            int? minimum = constructionYear.Minimum;
            int? maximum = constructionYear.Maximum;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.ConstructionYear.HasValue &&
                                (!minimum.HasValue ||
                                 property.ConstructionYear.Value >= minimum.Value) &&
                                (!maximum.HasValue ||
                                 property.ConstructionYear.Value <= maximum.Value)));
        }
        if (criteria?.Property?.ParkingSpaceCount is { } parkingSpaceCount)
        {
            int? minimum = parkingSpaceCount.Minimum;
            int? maximum = parkingSpaceCount.Maximum;

            query =
                query.Where(
                    listing =>
                        listing.SubjectType ==
                        MarketListingSubjectTypes.Property &&
                        _context.MarketProperties.Any(
                            property =>
                                property.Id == listing.SubjectId &&
                                property.ParkingSpaceCount.HasValue &&
                                (!minimum.HasValue ||
                                 property.ParkingSpaceCount.Value >= minimum.Value) &&
                                (!maximum.HasValue ||
                                 property.ParkingSpaceCount.Value <= maximum.Value)));
        }
        return query;
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
