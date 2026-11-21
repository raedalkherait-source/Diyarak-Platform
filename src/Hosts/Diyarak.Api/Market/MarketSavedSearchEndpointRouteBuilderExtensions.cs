using System.Text;
using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Market.Application.SavedSearches;
using Diyarak.Platform.BuildingBlocks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Diyarak.Api.Market;

/// <summary>Saved searches with in-site "new since last visit" counts (ADR-0066).</summary>
public static class MarketSavedSearchEndpointRouteBuilderExtensions
{
    private const string SavedSearchesPath = "/api/market/saved-searches";

    /// <summary>Filter keys of the public search; paging and anything else are dropped.</summary>
    private static readonly HashSet<string> FilterKeys = new(StringComparer.Ordinal)
    {
        "transactionIntent", "propertyCategory", "commercialSubtype",
        "city", "postalCode", "governorate", "district",
        "priceMin", "priceMax", "priceCurrency",
        "livingAreaMin", "livingAreaMax", "livingAreaUnit",
        "totalRoomsMin", "totalRoomsMax", "bedroomCountMin", "bedroomCountMax",
        "bathroomCountMin", "bathroomCountMax", "furnishingQuality", "propertyFeature",
        "constructionYearMin", "constructionYearMax", "parkingSpaceCountMin", "parkingSpaceCountMax",
        "legalTitleType", "finishingLevel", "floorMin", "floorMax",
    };

    public static IEndpointRouteBuilder MapMarketSavedSearchEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(SavedSearchesPath, ListAsync).RequireMappedUserManagement();
        endpoints.MapPost(SavedSearchesPath, CreateAsync).RequireMappedUserManagement();
        endpoints.MapPost($"{SavedSearchesPath}/{{searchId}}/seen", MarkSeenAsync).RequireMappedUserManagement();
        endpoints.MapDelete($"{SavedSearchesPath}/{{searchId}}", DeleteAsync).RequireMappedUserManagement();

        return endpoints;
    }

    /// <summary>
    /// Keeps only search filters, sorts them so equivalent searches compare equal, and checks that
    /// the public search accepts them. Returns <c>null</c> when the query is not a valid search.
    /// </summary>
    internal static string? Canonicalize(string? query)
    {
        if (query is null || query.Length > SavedSearchErrors.MaximumQueryLength)
            return null;

        Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(query);
        var canonical = new StringBuilder();

        foreach ((string key, StringValues values) in parsed
                     .Where(pair => FilterKeys.Contains(pair.Key))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (string? value in values.Order(StringComparer.Ordinal))
            {
                if (canonical.Length > 0)
                    canonical.Append('&');

                canonical.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value ?? string.Empty));
            }
        }

        string result = canonical.ToString();
        return result.Length <= SavedSearchErrors.MaximumQueryLength && Parse(result) is not null ? result : null;
    }

    /// <summary>Parses a stored canonical query into criteria; <c>null</c> if it is no longer valid.</summary>
    internal static PublishedListingSearchCriteria? Parse(string query)
    {
        var collection = new QueryCollection(QueryHelpers.ParseQuery(query));

        return PublicMarketListingEndpointRouteBuilderExtensions.TryParseSearchCriteria(
                collection,
                out PublishedListingSearchCriteria? criteria)
            ? criteria ?? new PublishedListingSearchCriteria()
            : null;
    }

    internal static async Task<IResult> ListAsync(
        IAuthenticatedActorAccessor actorAccessor,
        ListSavedSearchesUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<SavedSearchSummary>> result =
            await useCase.ExecuteAsync(GetRequiredActorUserId(actorAccessor), Parse, cancellationToken);

        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        httpContext.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new SavedSearchListResponse(
            [.. result.Value.Select(summary => new SavedSearchResponse(
                summary.Search.Id,
                summary.Search.Name,
                summary.Search.Query,
                summary.Search.CreatedAt,
                summary.Search.LastSeenAt,
                summary.NewListingCount,
                summary.NewListingCountCapped))]));
    }

    internal static async Task<IResult> CreateAsync(
        CreateSavedSearchRequest? request,
        IAuthenticatedActorAccessor actorAccessor,
        CreateSavedSearchUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string? query = Canonicalize(request?.Query);

        if (request is null || query is null)
            return ToProblemDetails(SavedSearchErrors.InvalidRequest, httpContext);

        Result<SavedSearch> result = await useCase.ExecuteAsync(
            GetRequiredActorUserId(actorAccessor),
            request.Name ?? string.Empty,
            query,
            cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"{SavedSearchesPath}/{result.Value.Id}",
                new SavedSearchResponse(result.Value.Id, result.Value.Name, result.Value.Query, result.Value.CreatedAt, result.Value.LastSeenAt, 0, false))
            : ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> MarkSeenAsync(
        string searchId,
        IAuthenticatedActorAccessor actorAccessor,
        MarkSavedSearchSeenUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(searchId, out Guid parsed) || parsed == Guid.Empty)
            return ToProblemDetails(SavedSearchErrors.InvalidRequest, httpContext);

        Result result = await useCase.ExecuteAsync(GetRequiredActorUserId(actorAccessor), parsed, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> DeleteAsync(
        string searchId,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteSavedSearchUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(searchId, out Guid parsed) || parsed == Guid.Empty)
            return ToProblemDetails(SavedSearchErrors.InvalidRequest, httpContext);

        Result result = await useCase.ExecuteAsync(GetRequiredActorUserId(actorAccessor), parsed, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : ToProblemDetails(result.Error, httpContext);
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
            _ => throw new InvalidOperationException($"Unsupported expected saved search error type '{error.Type}'."),
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

    internal sealed record CreateSavedSearchRequest(string? Name, string? Query);

    internal sealed record SavedSearchResponse(
        Guid SearchId,
        string Name,
        string Query,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastSeenAt,
        int NewListingCount,
        bool NewListingCountCapped);

    internal sealed record SavedSearchListResponse(IReadOnlyList<SavedSearchResponse> Items);
}
