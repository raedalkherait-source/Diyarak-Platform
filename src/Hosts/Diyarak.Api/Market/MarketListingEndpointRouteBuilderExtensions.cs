using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

public static class MarketListingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketListingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/publish",
                PublishListingAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        return endpoints;
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

        Guid? actorUserId = actorAccessor.UserId;

        if (actorUserId is null || actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");
        }

        Result result = await useCase.ExecuteAsync(
            parsedListingId,
            actorUserId.Value,
            cancellationToken);

        if (result.IsSuccess)
            return Results.NoContent();

        return ToProblemDetails(result.Error, httpContext);
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
                $"Unsupported expected publication error type '{error.Type}'."),
        };

        string title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => throw new InvalidOperationException(
                "Unsupported publication HTTP status code."),
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
}
