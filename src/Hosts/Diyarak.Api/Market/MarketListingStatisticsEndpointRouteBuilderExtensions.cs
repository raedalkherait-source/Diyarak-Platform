using System.Globalization;
using Diyarak.Api.Authentication;
using Diyarak.Api.RateLimiting;
using Diyarak.Market.Application.Statistics;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

/// <summary>Listing view beacons and the publisher's statistics (ADR-0067).</summary>
public static class MarketListingStatisticsEndpointRouteBuilderExtensions
{
    /// <summary>Anonymous view beacon.</summary>
    public static IEndpointRouteBuilder MapPublicListingViewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/api/market/public/listings/{listingId}/views",
                (string listingId, RecordListingViewUseCase useCase, CancellationToken cancellationToken) =>
                    RecordViewAsync(listingId, viewerUserId: null, useCase, cancellationToken))
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.ViewPolicy);

        return endpoints;
    }

    /// <summary>Signed-in view beacon (so publishers' own views are not counted) and owner statistics.</summary>
    public static IEndpointRouteBuilder MapMarketListingStatisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/api/market/listings/{listingId}/views",
                (string listingId, IAuthenticatedActorAccessor actor, RecordListingViewUseCase useCase, CancellationToken cancellationToken) =>
                    RecordViewAsync(listingId, actor.UserId, useCase, cancellationToken))
            .RequireMappedUserManagement()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.ViewPolicy);

        endpoints.MapGet("/api/market/listings/{listingId}/statistics", GetAsync).RequireMappedUserManagement();
        endpoints.MapGet("/api/market/listings/statistics", ListAsync).RequireMappedUserManagement();

        return endpoints;
    }

    /// <summary>Always 204: whether a view counted is not revealed to the caller.</summary>
    internal static async Task<IResult> RecordViewAsync(
        string listingId,
        Guid? viewerUserId,
        RecordListingViewUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(listingId, out Guid parsed))
            await useCase.ExecuteAsync(parsed, viewerUserId, cancellationToken);

        return Results.NoContent();
    }

    internal static async Task<IResult> GetAsync(
        string listingId,
        IAuthenticatedActorAccessor actor,
        GetListingStatisticsUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsed) || !TryParseDays(httpContext.Request.Query["days"], out int days))
            return ToProblemDetails(ListingStatisticsErrors.InvalidRequest, httpContext);

        Result<ListingStatisticsReport> result = await useCase.ExecuteAsync(parsed, RequireActor(actor), days, cancellationToken);
        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        httpContext.Response.Headers.CacheControl = "no-store";
        ListingStatisticsReport report = result.Value;

        return Results.Ok(new ListingStatisticsResponse(
            report.ListingId,
            report.From,
            report.To,
            report.Views,
            report.ContactReveals,
            report.Favorites,
            [.. report.Days.Select(day => new DailyStatisticsResponse(day.Day, day.Views, day.ContactReveals))]));
    }

    internal static async Task<IResult> ListAsync(
        IAuthenticatedActorAccessor actor,
        ListOwnedListingStatisticsUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseDays(httpContext.Request.Query["days"], out int days))
            return ToProblemDetails(ListingStatisticsErrors.InvalidRequest, httpContext);

        Result<IReadOnlyList<ListingStatisticsTotals>> result = await useCase.ExecuteAsync(RequireActor(actor), days, cancellationToken);
        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        httpContext.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new ListingTotalsListResponse(
            days,
            [.. result.Value.Select(total => new ListingTotalsResponse(total.ListingId, total.Views, total.ContactReveals))]));
    }

    private static bool TryParseDays(string? value, out int days)
    {
        if (string.IsNullOrEmpty(value))
        {
            days = 30;
            return true;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out days);
    }

    private static Guid RequireActor(IAuthenticatedActorAccessor actor) =>
        actor.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");

    private static IResult ToProblemDetails(Error error, HttpContext httpContext)
    {
        (int statusCode, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            _ => throw new InvalidOperationException($"Unsupported expected statistics error type '{error.Type}'."),
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

    internal sealed record DailyStatisticsResponse(DateOnly Day, int Views, int ContactReveals);

    internal sealed record ListingStatisticsResponse(
        Guid ListingId,
        DateOnly From,
        DateOnly To,
        int Views,
        int ContactReveals,
        int Favorites,
        IReadOnlyList<DailyStatisticsResponse> Days);

    internal sealed record ListingTotalsResponse(Guid ListingId, int Views, int ContactReveals);

    internal sealed record ListingTotalsListResponse(int Days, IReadOnlyList<ListingTotalsResponse> Items);
}
