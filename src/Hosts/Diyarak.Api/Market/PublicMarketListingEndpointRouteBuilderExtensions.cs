using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Market;

public static class PublicMarketListingEndpointRouteBuilderExtensions
{
    private const string PublicListingsPath = "/api/market/public/listings";

    private static readonly string[] PublicReadMethods =
    [
        HttpMethods.Get,
        HttpMethods.Head,
    ];

    public static IEndpointRouteBuilder MapPublicMarketListingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapMethods(
                PublicListingsPath,
                PublicReadMethods,
                ListPublishedListingsAsync)
            .AllowAnonymous();

        endpoints
            .MapMethods(
                $"{PublicListingsPath}/{{listingId}}",
                PublicReadMethods,
                GetPublishedListingAsync)
            .AllowAnonymous();

        return endpoints;
    }

    internal static async Task<IResult> ListPublishedListingsAsync(
        ListPublishedListingsUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SuppressResponseBodyForHead(httpContext);

        if (!TryParsePagination(
                httpContext.Request.Query,
                out int page,
                out int pageSize))
        {
            return ToProblemDetails(
                ListPublishedListingsErrors.InvalidPagination,
                httpContext);
        }
        if (!TryParseSearchCriteria(
                httpContext.Request.Query,
                out PublishedListingSearchCriteria? criteria))
        {
            return ToProblemDetails(
                ListPublishedListingsErrors.InvalidSearchCriteria,
                httpContext);
        }

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page,
                pageSize,
                criteria,
                cancellationToken);

        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        SetRevalidationCachePolicy(httpContext.Response);
        MarketPaginationLinkHeader.Set(
            httpContext.Response,
            PublicListingsPath,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.HasMore,
            httpContext.Request.Query);

        string entityTag =
            PublicMarketListingEntityTag.Create(result.Value);

        httpContext.Response.Headers["ETag"] = entityTag;

        if (PublicMarketListingEntityTag.MatchesIfNoneMatch(
                httpContext.Request.Headers,
                entityTag))
        {
            return Results.StatusCode(
                StatusCodes.Status304NotModified);
        }

        if (HttpMethods.IsHead(httpContext.Request.Method))
            return Results.Ok();

        PublicMarketListingResponse[] items = result.Value.Items
            .Select(ToResponse)
            .ToArray();

        return Results.Ok(
            new PublicMarketListingPageResponse(
                items,
                result.Value.Page,
                result.Value.PageSize,
                result.Value.HasMore));
    }

    internal static async Task<IResult> GetPublishedListingAsync(
        string listingId,
        GetPublishedListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SuppressResponseBodyForHead(httpContext);

        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            parsedListingId == Guid.Empty)
        {
            return ToProblemDetails(
                GetListingErrors.InvalidIdentifier,
                httpContext);
        }

        Result<PublishedListingProjection> result = await useCase.ExecuteAsync(
            parsedListingId,
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        SetRevalidationCachePolicy(httpContext.Response);

        string entityTag =
            PublicMarketListingEntityTag.Create(result.Value);

        httpContext.Response.Headers["ETag"] = entityTag;

        if (PublicMarketListingEntityTag.MatchesIfNoneMatch(
                httpContext.Request.Headers,
                entityTag))
        {
            return Results.StatusCode(
                StatusCodes.Status304NotModified);
        }

        if (HttpMethods.IsHead(httpContext.Request.Method))
            return Results.Ok();

        return Results.Ok(ToResponse(result.Value));
    }

    private static bool TryParseSearchCriteria(
        IQueryCollection query,
        out PublishedListingSearchCriteria? criteria)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!TryParseRepeatedEnumValues(
                query,
                "transactionIntent",
                out IReadOnlyCollection<Diyarak.Market.Listing.TransactionIntent>?
                    transactionIntents))
        {
            return Fail(out criteria);
        }

        if (!TryParseRepeatedEnumValues(
                query,
                "propertyCategory",
                out IReadOnlyCollection<Diyarak.Market.Property.PropertyCategory>?
                    categories))
        {
            return Fail(out criteria);
        }

        if (!TryParseRepeatedEnumValues(
                query,
                "commercialSubtype",
                out IReadOnlyCollection<Diyarak.Market.Property.CommercialPropertySubtype>?
                    commercialSubtypes))
        {
            return Fail(out criteria);
        }

        string? city = null;

        if (query.TryGetValue("city", out var cityValues))
        {
            if (cityValues.Count != 1 ||
                string.IsNullOrWhiteSpace(cityValues[0]))
            {
                return Fail(out criteria);
            }

            city = cityValues[0]!.Trim();
        }

        string? postalCode = null;

        if (query.TryGetValue("postalCode", out var postalCodeValues))
        {
            if (postalCodeValues.Count != 1 ||
                string.IsNullOrWhiteSpace(postalCodeValues[0]))
            {
                return Fail(out criteria);
            }

            postalCode = postalCodeValues[0]!.Trim();
        }

        decimal? priceMinimum = null;
        decimal? priceMaximum = null;
        Diyarak.Platform.Domain.Primitives.Currency? priceCurrency = null;
        bool hasPriceFilter = false;

        if (!TryParseOptionalDecimal(
                query,
                "priceMin",
                out priceMinimum,
                out bool hasPriceMinimum))
        {
            return Fail(out criteria);
        }

        hasPriceFilter = hasPriceFilter || hasPriceMinimum;

        if (!TryParseOptionalDecimal(
                query,
                "priceMax",
                out priceMaximum,
                out bool hasPriceMaximum))
        {
            return Fail(out criteria);
        }

        hasPriceFilter = hasPriceFilter || hasPriceMaximum;

        if (query.TryGetValue("priceCurrency", out var priceCurrencyValues))
        {
            if (priceCurrencyValues.Count != 1 ||
                string.IsNullOrWhiteSpace(priceCurrencyValues[0]) ||
                !Diyarak.Platform.Domain.Primitives.Currency.TryCreate(
                    priceCurrencyValues[0],
                    out priceCurrency))
            {
                return Fail(out criteria);
            }

            hasPriceFilter = true;
        }

        PublishedListingPriceSearchCriteria? price =
            hasPriceFilter
                ? new PublishedListingPriceSearchCriteria(
                    Minimum: priceMinimum,
                    Maximum: priceMaximum,
                    Currency: priceCurrency)
                : null;

        decimal? livingAreaMinimum = null;
        decimal? livingAreaMaximum = null;
        Diyarak.Platform.Domain.Primitives.AreaUnit? livingAreaUnit = null;
        bool hasLivingAreaFilter = false;

        if (!TryParseOptionalDecimal(
                query,
                "livingAreaMin",
                out livingAreaMinimum,
                out bool hasLivingAreaMinimum))
        {
            return Fail(out criteria);
        }

        hasLivingAreaFilter = hasLivingAreaFilter || hasLivingAreaMinimum;

        if (!TryParseOptionalDecimal(
                query,
                "livingAreaMax",
                out livingAreaMaximum,
                out bool hasLivingAreaMaximum))
        {
            return Fail(out criteria);
        }

        hasLivingAreaFilter = hasLivingAreaFilter || hasLivingAreaMaximum;

        if (query.TryGetValue("livingAreaUnit", out var livingAreaUnitValues))
        {
            if (livingAreaUnitValues.Count != 1 ||
                string.IsNullOrWhiteSpace(livingAreaUnitValues[0]) ||
                !Enum.TryParse(
                    livingAreaUnitValues[0],
                    ignoreCase: true,
                    out Diyarak.Platform.Domain.Primitives.AreaUnit parsedLivingAreaUnit) ||
                !Enum.IsDefined(parsedLivingAreaUnit))
            {
                return Fail(out criteria);
            }

            livingAreaUnit = parsedLivingAreaUnit;
            hasLivingAreaFilter = true;
        }

        PublishedAreaSearchCriteria? livingArea =
            hasLivingAreaFilter
                ? new PublishedAreaSearchCriteria(
                    Minimum: livingAreaMinimum,
                    Maximum: livingAreaMaximum,
                    Unit: livingAreaUnit)
                : null;

        decimal? totalRoomsMinimum = null;
        decimal? totalRoomsMaximum = null;
        bool hasTotalRoomsFilter = false;

        if (!TryParseOptionalDecimal(
                query,
                "totalRoomsMin",
                out totalRoomsMinimum,
                out bool hasTotalRoomsMinimum))
        {
            return Fail(out criteria);
        }

        hasTotalRoomsFilter = hasTotalRoomsFilter || hasTotalRoomsMinimum;

        if (!TryParseOptionalDecimal(
                query,
                "totalRoomsMax",
                out totalRoomsMaximum,
                out bool hasTotalRoomsMaximum))
        {
            return Fail(out criteria);
        }

        hasTotalRoomsFilter = hasTotalRoomsFilter || hasTotalRoomsMaximum;

        PublishedDecimalRange? totalRooms =
            hasTotalRoomsFilter
                ? new PublishedDecimalRange(
                    Minimum: totalRoomsMinimum,
                    Maximum: totalRoomsMaximum)
                : null;

        PublishedPropertyRoomSearchCriteria? rooms =
            totalRooms is null
                ? null
                : new PublishedPropertyRoomSearchCriteria(
                    TotalRooms: totalRooms);
        int? bedroomCountMinimum = null;
        int? bedroomCountMaximum = null;
        bool hasBedroomCountFilter = false;

        if (!TryParseOptionalInteger(
                query,
                "bedroomCountMin",
                out bedroomCountMinimum,
                out bool hasBedroomCountMinimum))
        {
            return Fail(out criteria);
        }

        hasBedroomCountFilter = hasBedroomCountFilter || hasBedroomCountMinimum;

        if (!TryParseOptionalInteger(
                query,
                "bedroomCountMax",
                out bedroomCountMaximum,
                out bool hasBedroomCountMaximum))
        {
            return Fail(out criteria);
        }

        hasBedroomCountFilter = hasBedroomCountFilter || hasBedroomCountMaximum;

        if (hasBedroomCountFilter)
        {
            rooms = rooms is null
                ? new PublishedPropertyRoomSearchCriteria(
                    BedroomCount: new PublishedIntegerRange(
                        Minimum: bedroomCountMinimum,
                        Maximum: bedroomCountMaximum))
                : rooms with
                {
                    BedroomCount = new PublishedIntegerRange(
                        Minimum: bedroomCountMinimum,
                        Maximum: bedroomCountMaximum)
                };
        }
        int? bathroomCountMinimum = null;
        int? bathroomCountMaximum = null;
        bool hasBathroomCountFilter = false;

        if (!TryParseOptionalInteger(
                query,
                "bathroomCountMin",
                out bathroomCountMinimum,
                out bool hasBathroomCountMinimum))
        {
            return Fail(out criteria);
        }

        hasBathroomCountFilter = hasBathroomCountFilter || hasBathroomCountMinimum;

        if (!TryParseOptionalInteger(
                query,
                "bathroomCountMax",
                out bathroomCountMaximum,
                out bool hasBathroomCountMaximum))
        {
            return Fail(out criteria);
        }

        hasBathroomCountFilter = hasBathroomCountFilter || hasBathroomCountMaximum;

        if (hasBathroomCountFilter)
        {
            rooms = rooms is null
                ? new PublishedPropertyRoomSearchCriteria(
                    BathroomCount: new PublishedIntegerRange(
                        Minimum: bathroomCountMinimum,
                        Maximum: bathroomCountMaximum))
                : rooms with
                {
                    BathroomCount = new PublishedIntegerRange(
                        Minimum: bathroomCountMinimum,
                        Maximum: bathroomCountMaximum)
                };
        }
        int? parkingSpaceCountMinimum = null;
        int? parkingSpaceCountMaximum = null;
        bool hasParkingSpaceCountFilter = false;

        if (!TryParseOptionalInteger(
                query,
                "parkingSpaceCountMin",
                out parkingSpaceCountMinimum,
                out bool hasParkingSpaceCountMinimum))
        {
            return Fail(out criteria);
        }

        hasParkingSpaceCountFilter = hasParkingSpaceCountFilter || hasParkingSpaceCountMinimum;

        if (!TryParseOptionalInteger(
                query,
                "parkingSpaceCountMax",
                out parkingSpaceCountMaximum,
                out bool hasParkingSpaceCountMaximum))
        {
            return Fail(out criteria);
        }

        hasParkingSpaceCountFilter = hasParkingSpaceCountFilter || hasParkingSpaceCountMaximum;

        PublishedIntegerRange? parkingSpaceCount =
            hasParkingSpaceCountFilter
                ? new PublishedIntegerRange(
                    Minimum: parkingSpaceCountMinimum,
                    Maximum: parkingSpaceCountMaximum)
                : null;
        int? constructionYearMinimum = null;
        int? constructionYearMaximum = null;
        bool hasConstructionYearFilter = false;

        if (!TryParseOptionalInteger(
                query,
                "constructionYearMin",
                out constructionYearMinimum,
                out bool hasConstructionYearMinimum))
        {
            return Fail(out criteria);
        }

        hasConstructionYearFilter = hasConstructionYearFilter || hasConstructionYearMinimum;

        if (!TryParseOptionalInteger(
                query,
                "constructionYearMax",
                out constructionYearMaximum,
                out bool hasConstructionYearMaximum))
        {
            return Fail(out criteria);
        }

        hasConstructionYearFilter = hasConstructionYearFilter || hasConstructionYearMaximum;

        PublishedIntegerRange? constructionYear =
            hasConstructionYearFilter
                ? new PublishedIntegerRange(
                    Minimum: constructionYearMinimum,
                    Maximum: constructionYearMaximum)
                : null;
        if (!TryParseRepeatedEnumValues(
                query,
                "furnishingQuality",
                out IReadOnlyCollection<Diyarak.Market.Property.FurnishingQuality>?
                    furnishingQualities))
        {
            return Fail(out criteria);
        }
        if (!TryParseRepeatedEnumValues(
                query,
                "propertyFeature",
                out IReadOnlyCollection<Diyarak.Market.Property.PropertyFeature>?
                    requiredFeatures))
        {
            return Fail(out criteria);
        }
        if (transactionIntents is null &&
            categories is null &&
            commercialSubtypes is null &&
            city is null &&
            postalCode is null &&
            price is null &&
            livingArea is null &&
            rooms is null &&
            furnishingQualities is null &&
            requiredFeatures is null &&
            constructionYear is null &&
            parkingSpaceCount is null)
        {
            criteria = null;
            return true;
        }

        PublishedPropertyLocationSearchCriteria? location =
            city is null &&
            postalCode is null
                ? null
                : new PublishedPropertyLocationSearchCriteria(
                    City: city,
                    PostalCode: postalCode);

        PublishedPropertySearchCriteria? property =
            categories is null &&
            commercialSubtypes is null &&
            location is null &&
            livingArea is null &&
            rooms is null &&
            furnishingQualities is null &&
            requiredFeatures is null &&
            constructionYear is null &&
            parkingSpaceCount is null
                ? null
                : new PublishedPropertySearchCriteria(
                    Categories: categories,
                    CommercialSubtypes: commercialSubtypes,
                    Location: location,
                    LivingArea: livingArea,
                    Rooms: rooms,
                    FurnishingQualities: furnishingQualities,
                    RequiredFeatures: requiredFeatures,
                    ConstructionYear: constructionYear,
                    ParkingSpaceCount: parkingSpaceCount);

        criteria =
            new PublishedListingSearchCriteria(
                TransactionIntents: transactionIntents,
                Price: price,
                Property: property);

        return true;
    }
    private static bool TryParseOptionalInteger(
        IQueryCollection query,
        string name,
        out int? value,
        out bool isPresent)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        value = null;
        isPresent = false;

        if (!query.TryGetValue(name, out var rawValues))
            return true;

        if (rawValues.Count != 1 ||
            string.IsNullOrWhiteSpace(rawValues[0]) ||
            !int.TryParse(
                rawValues[0],
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsedValue))
        {
            return false;
        }

        value = parsedValue;
        isPresent = true;
        return true;
    }
    private static bool TryParseOptionalDecimal(
        IQueryCollection query,
        string name,
        out decimal? value,
        out bool isPresent)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        value = null;
        isPresent = false;

        if (!query.TryGetValue(name, out var rawValues))
            return true;

        if (rawValues.Count != 1 ||
            string.IsNullOrWhiteSpace(rawValues[0]) ||
            !decimal.TryParse(
                rawValues[0],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal parsedValue))
        {
            return false;
        }

        value = parsedValue;
        isPresent = true;
        return true;
    }
    private static bool TryParseRepeatedEnumValues<TEnum>(
        IQueryCollection query,
        string name,
        out IReadOnlyCollection<TEnum>? values)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        values = null;

        if (!query.TryGetValue(name, out var rawValues))
            return true;

        if (rawValues.Count == 0)
            return false;

        var parsedValues = new List<TEnum>(rawValues.Count);

        foreach (string? value in rawValues)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out TEnum parsedValue) ||
                !Enum.IsDefined(parsedValue))
            {
                return false;
            }

            parsedValues.Add(parsedValue);
        }

        values = parsedValues;
        return true;
    }
    private static bool Fail(
        out PublishedListingSearchCriteria? criteria)
    {
        criteria = null;
        return false;
    }
    private static bool TryParsePagination(
        IQueryCollection query,
        out int page,
        out int pageSize)
    {
        ArgumentNullException.ThrowIfNull(query);

        page = ListPublishedListingsUseCase.DefaultPage;
        pageSize = ListPublishedListingsUseCase.DefaultPageSize;

        if (query.TryGetValue("page", out var pageValues) &&
            (pageValues.Count != 1 ||
             !int.TryParse(pageValues[0], out page)))
        {
            return false;
        }

        if (query.TryGetValue("pageSize", out var pageSizeValues) &&
            (pageSizeValues.Count != 1 ||
             !int.TryParse(pageSizeValues[0], out pageSize)))
        {
            return false;
        }

        return page > 0 &&
            pageSize > 0 &&
            pageSize <= ListPublishedListingsUseCase.MaximumPageSize;
    }

    private static PublicMarketListingResponse ToResponse(
        PublishedListingProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        MarketListing listing = projection.Listing;
        MarketProperty property = projection.Property;

        if (listing.Context is not { } context ||
            listing.Headline is not { } headline ||
            listing.Price is not { } price)
        {
            throw new InvalidOperationException(
                "A published Listing must contain context, headline, and price.");
        }

        return new PublicMarketListingResponse(
            listing.Id,
            new PublicMarketListingContextResponse(
                context.PublishingRole.ToString(),
                context.TransactionIntent.ToString()),
            headline.Value,
            new PublicMarketListingPriceResponse(
                price.IsOnRequest,
                price.Amount?.Amount,
                price.Amount?.Currency.Code),
            ToPropertyResponse(property),
            listing.AvailableFromDate?.Value);
    }

    private static PublicMarketListingPropertyResponse ToPropertyResponse(
        MarketProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        PublicMarketListingAreaResponse? livingArea =
            property.LivingArea is { } area
                ? new PublicMarketListingAreaResponse(
                    area.Value,
                    area.Unit.ToString())
                : null;

        return new PublicMarketListingPropertyResponse(
            property.Category.ToString(),
            new PublicMarketListingPropertyLocationResponse(
                property.Address.City,
                property.Address.PostalCode),
            livingArea,
            property.TotalRooms,
            property.BedroomCount,
            property.BathroomCount,
            property.FurnishingQuality?.ToString(),
            property.Features
                .Select(static feature => feature.ToString())
                .ToArray(),
            property.ConstructionYear,
            property.CommercialSubtype?.ToString(),
            property.ParkingSpaceCount);
    }

    private static IResult ToProblemDetails(
        Error error,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        SetErrorCachePolicy(httpContext.Response);

        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => throw new InvalidOperationException(
                $"Unsupported expected public Market Listing error type '{error.Type}'."),
        };

        if (HttpMethods.IsHead(httpContext.Request.Method))
            return Results.StatusCode(statusCode);

        string title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            _ => throw new InvalidOperationException(
                "Unsupported public Market Listing HTTP status code."),
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier,
            });
    }


    private static void SuppressResponseBodyForHead(
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (HttpMethods.IsHead(httpContext.Request.Method))
            httpContext.Response.Body = System.IO.Stream.Null;
    }

    private static void SetRevalidationCachePolicy(
        HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers["Cache-Control"] = "public, no-cache";
    }

    private static void SetErrorCachePolicy(
        HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers["Cache-Control"] = "no-store";
    }

    internal sealed record PublicMarketListingPageResponse(
        PublicMarketListingResponse[] Items,
        int Page,
        int PageSize,
        bool HasMore);

    internal sealed record PublicMarketListingResponse(
        Guid ListingId,
        PublicMarketListingContextResponse Context,
        string Headline,
        PublicMarketListingPriceResponse Price,
        PublicMarketListingPropertyResponse Property,
        DateOnly? AvailableFromDate);

    internal sealed record PublicMarketListingContextResponse(
        string PublishingRole,
        string TransactionIntent);

    internal sealed record PublicMarketListingPriceResponse(
        bool IsOnRequest,
        decimal? Amount,
        string? Currency);

    internal sealed record PublicMarketListingPropertyResponse(
        string Category,
        PublicMarketListingPropertyLocationResponse Location,
        PublicMarketListingAreaResponse? LivingArea,
        decimal? TotalRooms,
        int? BedroomCount,
        int? BathroomCount,
        string? FurnishingQuality,
        string[] Features,
        int? ConstructionYear,
        string? CommercialSubtype,
        int? ParkingSpaceCount);

    internal sealed record PublicMarketListingPropertyLocationResponse(
        string City,
        string PostalCode);

    internal sealed record PublicMarketListingAreaResponse(
        decimal Value,
        string Unit);
}
