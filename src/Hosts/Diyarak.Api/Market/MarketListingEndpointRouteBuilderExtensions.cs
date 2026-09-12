using System.Text.Json;
using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Market;

public static class MarketListingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketListingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost(
                "/api/market/listings",
                CreateListingAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        endpoints
            .MapMethods(
                "/api/market/listings/{listingId}",
                new[] { HttpMethods.Patch },
                UpdateListingAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/publish",
                PublishListingAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        return endpoints;
    }

    internal static async Task<IResult> CreateListingAsync(
        CreateMarketListingRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        CreateListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            !Guid.TryParse(request.PropertyId, out Guid propertyId) ||
            propertyId == Guid.Empty)
        {
            return ToProblemDetails(
                CreateListingErrors.InvalidPropertyIdentifier,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<Guid> result = await useCase.ExecuteAsync(
            propertyId,
            actorUserId,
            cancellationToken);

        if (result.IsSuccess)
        {
            Guid listingId = result.Value;

            return Results.Created(
                $"/api/market/listings/{listingId}",
                new CreateMarketListingResponse(
                    listingId,
                    MarketListing.InitialVersion));
        }

        return ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> UpdateListingAsync(
        string listingId,
        JsonElement request,
        IAuthenticatedActorAccessor actorAccessor,
        UpdateListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            parsedListingId == Guid.Empty)
        {
            return ToProblemDetails(
                UpdateListingErrors.InvalidIdentifier,
                httpContext);
        }

        if (!MarketListingUpdateRequestParser.TryParse(
                request,
                out UpdateListingPatch patch))
        {
            return ToProblemDetails(
                UpdateListingErrors.InvalidPatch,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<long> result = await useCase.ExecuteAsync(
            parsedListingId,
            actorUserId,
            patch,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(
                new UpdateMarketListingResponse(
                    parsedListingId,
                    result.Value));
        }

        return ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> PublishListingAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        PublishListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            parsedListingId == Guid.Empty)
        {
            return ToProblemDetails(
                PublishListingErrors.InvalidIdentifier,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result result = await useCase.ExecuteAsync(
            parsedListingId,
            actorUserId,
            cancellationToken);

        if (result.IsSuccess)
            return Results.NoContent();

        return ToProblemDetails(result.Error, httpContext);
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
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new InvalidOperationException(
                $"Unsupported expected Market Listing error type '{error.Type}'."),
        };

        string title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => throw new InvalidOperationException(
                "Unsupported Market Listing HTTP status code."),
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

    internal sealed record CreateMarketListingRequest(
        string? PropertyId);

    internal sealed record CreateMarketListingResponse(
        Guid ListingId,
        long Version);

    internal sealed record UpdateMarketListingResponse(
        Guid ListingId,
        long Version);
}
