using System.Globalization;
using Diyarak.Api.Authentication;
using Diyarak.Market.Application.Favorites;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

/// <summary>Saved Listings of the signed-in user (ADR-0064).</summary>
public static class MarketFavoriteEndpointRouteBuilderExtensions
{
    private const string FavoritesPath = "/api/market/favorites";

    public static IEndpointRouteBuilder MapMarketFavoriteEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(FavoritesPath, ListAsync).RequireMappedUserManagement();
        endpoints.MapGet($"{FavoritesPath}/{{listingId}}", GetStatusAsync).RequireMappedUserManagement();
        endpoints.MapPut($"{FavoritesPath}/{{listingId}}", AddAsync).RequireMappedUserManagement();
        endpoints.MapDelete($"{FavoritesPath}/{{listingId}}", RemoveAsync).RequireMappedUserManagement();

        return endpoints;
    }

    internal static async Task<IResult> ListAsync(
        IAuthenticatedActorAccessor actorAccessor,
        ListListingFavoritesUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParsePositive(httpContext.Request.Query["page"], 1, out int page) ||
            !TryParsePositive(httpContext.Request.Query["pageSize"], 20, out int pageSize))
        {
            return ToProblemDetails(ListingFavoriteErrors.InvalidRequest, httpContext);
        }

        Result<FavoriteListingPage> result = await useCase.ExecuteAsync(
            GetRequiredActorUserId(actorAccessor),
            page,
            pageSize,
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        httpContext.Response.Headers.CacheControl = "no-store";

        return Results.Ok(
            new PublicMarketListingEndpointRouteBuilderExtensions.PublicMarketListingPageResponse(
                [.. result.Value.Items.Select(PublicMarketListingEndpointRouteBuilderExtensions.ToResponse)],
                result.Value.Page,
                result.Value.PageSize,
                result.Value.HasMore));
    }

    internal static async Task<IResult> GetStatusAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        GetListingFavoriteStatusUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseListingId(listingId, out Guid parsed))
            return ToProblemDetails(ListingFavoriteErrors.InvalidRequest, httpContext);

        Result<bool> result = await useCase.ExecuteAsync(parsed, GetRequiredActorUserId(actorAccessor), cancellationToken);

        httpContext.Response.Headers.CacheControl = "no-store";

        return result.IsSuccess
            ? Results.Ok(new FavoriteStatusResponse(parsed, result.Value))
            : ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> AddAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        AddListingFavoriteUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseListingId(listingId, out Guid parsed))
            return ToProblemDetails(ListingFavoriteErrors.InvalidRequest, httpContext);

        Result result = await useCase.ExecuteAsync(parsed, GetRequiredActorUserId(actorAccessor), cancellationToken);

        return result.IsSuccess ? Results.NoContent() : ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> RemoveAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        RemoveListingFavoriteUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseListingId(listingId, out Guid parsed))
            return ToProblemDetails(ListingFavoriteErrors.InvalidRequest, httpContext);

        Result result = await useCase.ExecuteAsync(parsed, GetRequiredActorUserId(actorAccessor), cancellationToken);

        return result.IsSuccess ? Results.NoContent() : ToProblemDetails(result.Error, httpContext);
    }

    private static bool TryParseListingId(string value, out Guid listingId) =>
        Guid.TryParse(value, out listingId) && listingId != Guid.Empty;

    private static bool TryParsePositive(string? value, int fallback, out int parsed)
    {
        if (string.IsNullOrEmpty(value))
        {
            parsed = fallback;
            return true;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed > 0;
    }

    private static Guid GetRequiredActorUserId(IAuthenticatedActorAccessor actorAccessor)
    {
        ArgumentNullException.ThrowIfNull(actorAccessor);

        return actorAccessor.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");
    }

    private static IResult ToProblemDetails(Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        (int statusCode, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => throw new InvalidOperationException($"Unsupported expected favorite error type '{error.Type}'."),
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

    internal sealed record FavoriteStatusResponse(Guid ListingId, bool IsFavorite);
}
