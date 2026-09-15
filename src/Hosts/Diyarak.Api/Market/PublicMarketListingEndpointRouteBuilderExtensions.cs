using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Market;

public static class PublicMarketListingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPublicMarketListingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapGet(
                "/api/market/public/listings/{listingId}",
                GetPublishedListingAsync)
            .AllowAnonymous();

        return endpoints;
    }

    internal static async Task<IResult> GetPublishedListingAsync(
        string listingId,
        GetPublishedListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            parsedListingId == Guid.Empty)
        {
            return ToProblemDetails(
                GetListingErrors.InvalidIdentifier,
                httpContext);
        }

        Result<MarketListing> result = await useCase.ExecuteAsync(
            parsedListingId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value))
            : ToProblemDetails(result.Error, httpContext);
    }

    private static PublicMarketListingResponse ToResponse(
        MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

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
            listing.AvailableFromDate?.Value);
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
                $"Unsupported expected public Market Listing error type '{error.Type}'."),
        };

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

    internal sealed record PublicMarketListingResponse(
        Guid ListingId,
        PublicMarketListingContextResponse Context,
        string Headline,
        PublicMarketListingPriceResponse Price,
        DateOnly? AvailableFromDate);

    internal sealed record PublicMarketListingContextResponse(
        string PublishingRole,
        string TransactionIntent);

    internal sealed record PublicMarketListingPriceResponse(
        bool IsOnRequest,
        decimal? Amount,
        string? Currency);
}
