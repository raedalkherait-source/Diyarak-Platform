using System.Text.Json;
using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
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
            .RequireMappedUserManagement();

        endpoints
            .MapGet(
                "/api/market/listings",
                ListOwnedListingsAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapGet(
                "/api/market/listings/{listingId}",
                GetListingAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapMethods(
                "/api/market/listings/{listingId}",
                new[] { HttpMethods.Patch },
                UpdateListingAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/publish",
                PublishListingAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/pause",
                (string listingId, IAuthenticatedActorAccessor actorAccessor, ChangeListingStatusUseCase useCase, HttpContext httpContext, CancellationToken cancellationToken) =>
                    ChangeStatusAsync(listingId, ListingStatusChange.Pause, null, actorAccessor, useCase, httpContext, cancellationToken))
            .RequireMappedUserManagement();

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/resume",
                (string listingId, IAuthenticatedActorAccessor actorAccessor, ChangeListingStatusUseCase useCase, HttpContext httpContext, CancellationToken cancellationToken) =>
                    ChangeStatusAsync(listingId, ListingStatusChange.Resume, null, actorAccessor, useCase, httpContext, cancellationToken))
            .RequireMappedUserManagement();

        endpoints
            .MapPost(
                "/api/market/listings/{listingId}/close",
                (string listingId, CloseMarketListingRequest? request, IAuthenticatedActorAccessor actorAccessor, ChangeListingStatusUseCase useCase, HttpContext httpContext, CancellationToken cancellationToken) =>
                    Enum.TryParse(request?.Reason, ignoreCase: false, out ListingClosureReason reason) && Enum.IsDefined(reason)
                        ? ChangeStatusAsync(listingId, ListingStatusChange.Close, reason, actorAccessor, useCase, httpContext, cancellationToken)
                        : Task.FromResult(ToProblemDetails(ChangeListingStatusErrors.InvalidRequest, httpContext)))
            .RequireMappedUserManagement();

        endpoints
            .MapDelete(
                "/api/market/listings/{listingId}",
                DeleteDraftListingAsync)
            .RequireMappedUserManagement();

        return endpoints;
    }

    /// <summary>Deletes a draft. The expected version is passed as <c>?version=</c>.</summary>
    internal static async Task<IResult> DeleteDraftListingAsync(
        string listingId,
        string? version,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteDraftListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            parsedListingId == Guid.Empty ||
            !long.TryParse(version, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long expectedVersion) ||
            expectedVersion <= 0)
        {
            return ToProblemDetails(DeleteDraftListingErrors.InvalidRequest, httpContext);
        }

        Result result = await useCase.ExecuteAsync(
            parsedListingId,
            GetRequiredActorUserId(actorAccessor),
            expectedVersion,
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : ToProblemDetails(result.Error, httpContext);
    }

    internal static async Task<IResult> ChangeStatusAsync(
        string listingId,
        ListingStatusChange change,
        ListingClosureReason? reason,
        IAuthenticatedActorAccessor actorAccessor,
        ChangeListingStatusUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) || parsedListingId == Guid.Empty)
            return ToProblemDetails(ChangeListingStatusErrors.InvalidRequest, httpContext);

        Result<long> result = await useCase.ExecuteAsync(
            parsedListingId,
            GetRequiredActorUserId(actorAccessor),
            change,
            reason,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new UpdateMarketListingResponse(parsedListingId, result.Value))
            : ToProblemDetails(result.Error, httpContext);
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

    internal static async Task<IResult> ListOwnedListingsAsync(
        IAuthenticatedActorAccessor actorAccessor,
        ListOwnedListingsUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParsePagination(
                httpContext.Request.Query,
                out int page,
                out int pageSize))
        {
            return ToProblemDetails(
                ListOwnedListingsErrors.InvalidPagination,
                httpContext);
        }

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<OwnedListingPage> result =
            await useCase.ExecuteAsync(
                actorUserId,
                page,
                pageSize,
                cancellationToken);

        if (!result.IsSuccess)
            return ToProblemDetails(result.Error, httpContext);

        MarketPaginationLinkHeader.Set(
            httpContext.Response,
            "/api/market/listings",
            result.Value.Page,
            result.Value.PageSize,
            result.Value.HasMore);

        MarketListingResponse[] items = result.Value.Items
            .Select(ToResponse)
            .ToArray();

        return Results.Ok(
            new MarketListingPageResponse(
                items,
                result.Value.Page,
                result.Value.PageSize,
                result.Value.HasMore));
    }

    internal static async Task<IResult> GetListingAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        GetListingUseCase useCase,
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

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        Result<MarketListing> result = await useCase.ExecuteAsync(
            parsedListingId,
            actorUserId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value))
            : ToProblemDetails(result.Error, httpContext);
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

    private static bool TryParsePagination(
        IQueryCollection query,
        out int page,
        out int pageSize)
    {
        ArgumentNullException.ThrowIfNull(query);

        page = ListOwnedListingsUseCase.DefaultPage;
        pageSize = ListOwnedListingsUseCase.DefaultPageSize;

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
            pageSize <= ListOwnedListingsUseCase.MaximumPageSize;
    }

    private static MarketListingResponse ToResponse(
        MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        return new MarketListingResponse(
            listing.Id,
            listing.Version,
            listing.Status.ToString(),
            new MarketListingSubjectResponse(
                listing.SubjectReference.SubjectId,
                listing.SubjectReference.SubjectType),
            listing.Context is { } context
                ? new MarketListingContextResponse(
                    context.PublishingRole.ToString(),
                    context.TransactionIntent.ToString())
                : null,
            listing.Headline?.Value,
            listing.Price is { } price
                ? new MarketListingPriceResponse(
                    price.IsOnRequest,
                    price.Amount?.Amount,
                    price.Amount?.Currency.Code)
                : null,
            listing.AvailableFromDate?.Value,
            listing.Contact is { } contact
                ? new MarketListingContactOptionsResponse(contact.AllowCalls, contact.AllowWhatsApp)
                : null,
            listing.ClosureReason?.ToString(),
            listing.ModerationNote);
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

    internal sealed record MarketListingResponse(
        Guid ListingId,
        long Version,
        string Status,
        MarketListingSubjectResponse Subject,
        MarketListingContextResponse? Context,
        string? Headline,
        MarketListingPriceResponse? Price,
        DateOnly? AvailableFromDate,
        MarketListingContactOptionsResponse? Contact,
        string? ClosureReason,
        string? ModerationNote);

    internal sealed record CloseMarketListingRequest(
        string? Reason);

    internal sealed record MarketListingContactOptionsResponse(
        bool AllowCalls,
        bool AllowWhatsApp);

    internal sealed record MarketListingSubjectResponse(
        Guid SubjectId,
        string SubjectType);

    internal sealed record MarketListingContextResponse(
        string PublishingRole,
        string TransactionIntent);

    internal sealed record MarketListingPriceResponse(
        bool IsOnRequest,
        decimal? Amount,
        string? Currency);

    internal sealed record MarketListingPageResponse(
        IReadOnlyCollection<MarketListingResponse> Items,
        int Page,
        int PageSize,
        bool HasMore);

    internal sealed record UpdateMarketListingResponse(
        Guid ListingId,
        long Version);
}
