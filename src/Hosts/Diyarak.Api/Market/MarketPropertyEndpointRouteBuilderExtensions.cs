using System.Text.Json;
using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Market;

public static class MarketPropertyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketPropertyEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost(
                "/api/market/properties",
                CreatePropertyAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        endpoints
            .MapGet(
                "/api/market/properties/{propertyId}",
                GetPropertyAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        return endpoints;
    }

    internal static async Task<IResult> CreatePropertyAsync(
        JsonElement request,
        IAuthenticatedActorAccessor actorAccessor,
        CreatePropertyUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!MarketPropertyCreateRequestParser.TryParse(
                request,
                out CreatePropertyCommand command))
        {
            return ToProblemDetails(
                CreatePropertyErrors.InvalidRequest,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<Guid> result = await useCase.ExecuteAsync(
            command,
            actorUserId,
            cancellationToken);

        if (result.IsSuccess)
        {
            Guid propertyId = result.Value;

            return Results.Created(
                $"/api/market/properties/{propertyId}",
                new CreateMarketPropertyResponse(propertyId));
        }

        return ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> GetPropertyAsync(
        string propertyId,
        IAuthenticatedActorAccessor actorAccessor,
        GetPropertyUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(propertyId, out Guid parsedPropertyId) ||
            parsedPropertyId == Guid.Empty)
        {
            return ToProblemDetails(
                GetPropertyErrors.InvalidIdentifier,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<MarketProperty> result = await useCase.ExecuteAsync(
            parsedPropertyId,
            actorUserId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value))
            : ToProblemDetails(result.Error, httpContext);
    }

    private static Guid GetRequiredActorUserId(
        IAuthenticatedActorAccessor actorAccessor)
    {
        ArgumentNullException.ThrowIfNull(actorAccessor);

        Guid? actorUserId = actorAccessor.UserId;

        if (actorUserId is null || actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");
        }

        return actorUserId.Value;
    }

    private static MarketPropertyResponse ToResponse(
        MarketProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return new MarketPropertyResponse(
            property.Id,
            property.Category.ToString(),
            new MarketPropertyAddressResponse(
                property.Address.Street,
                property.Address.HouseNumber,
                property.Address.PostalCode,
                property.Address.City,
                property.Address.Location is { } location
                    ? new MarketGeoCoordinateResponse(
                        location.Latitude,
                        location.Longitude)
                    : null),
            ToAreaResponse(property.LivingArea),
            ToAreaResponse(property.UsableArea),
            property.TotalRooms,
            property.BedroomCount,
            property.BathroomCount,
            property.FurnishingQuality?.ToString(),
            property.Features
                .Select(static feature => feature.ToString())
                .ToArray(),
            property.ConstructionYear,
            property.LastModernizationYear,
            property.CommercialSubtype?.ToString(),
            ToAreaResponse(property.SalesArea),
            ToAreaResponse(property.TotalArea),
            property.ParkingSpaceCount);
    }

    private static MarketAreaResponse? ToAreaResponse(
        Area? area)
    {
        return area is { } value
            ? new MarketAreaResponse(
                value.Value,
                value.Unit.ToString())
            : null;
    }

    private static IResult ToProblemDetails(
        Error error,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => throw new InvalidOperationException(
                $"Unsupported expected Market Property error type '{error.Type}'."),
        };

        string title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            _ => throw new InvalidOperationException(
                "Unsupported Market Property HTTP status code."),
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

    internal sealed record CreateMarketPropertyResponse(
        Guid PropertyId);

    internal sealed record MarketPropertyResponse(
        Guid PropertyId,
        string Category,
        MarketPropertyAddressResponse Address,
        MarketAreaResponse? LivingArea,
        MarketAreaResponse? UsableArea,
        decimal? TotalRooms,
        int? BedroomCount,
        int? BathroomCount,
        string? FurnishingQuality,
        IReadOnlyCollection<string> Features,
        int? ConstructionYear,
        int? LastModernizationYear,
        string? CommercialSubtype,
        MarketAreaResponse? SalesArea,
        MarketAreaResponse? TotalArea,
        int? ParkingSpaceCount);

    internal sealed record MarketPropertyAddressResponse(
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        MarketGeoCoordinateResponse? Location);

    internal sealed record MarketGeoCoordinateResponse(
        double Latitude,
        double Longitude);

    internal sealed record MarketAreaResponse(
        decimal Value,
        string Unit);
}
