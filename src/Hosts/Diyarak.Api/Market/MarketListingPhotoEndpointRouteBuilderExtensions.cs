using Diyarak.Api.Authentication;
using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Diyarak.Api.Market;

internal static class MarketListingPhotoEndpointRouteBuilderExtensions
{
    private const string PublicCacheControl = "public, max-age=31536000, immutable";

    internal static IEndpointRouteBuilder MapMarketListingPhotoManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/api/market/listings/{listingId}/photos", UploadAsync)
            .RequireMappedUserManagement()
            .WithMetadata(new RequestSizeLimitAttribute(ListingPhoto.MaximumSizeBytes + (64 * 1024)));

        endpoints
            .MapGet("/api/market/listings/{listingId}/photos", ListAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapGet("/api/market/listings/{listingId}/photos/{photoId}/content", GetOwnerContentAsync)
            .RequireMappedUserManagement();

        endpoints
            .MapDelete("/api/market/listings/{listingId}/photos/{photoId}", DeleteAsync)
            .RequireMappedUserManagement();

        return endpoints;
    }

    internal static IEndpointRouteBuilder MapPublicMarketListingPhotoEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapMethods(
                "/api/market/public/listings/{listingId}/photos/{photoId}",
                [HttpMethods.Get, HttpMethods.Head],
                GetPublicContentAsync)
            .AllowAnonymous();

        return endpoints;
    }

    internal static string PublicPhotoUrl(Guid listingId, Guid photoId) =>
        $"/api/market/public/listings/{listingId}/photos/{photoId}";

    internal static string OwnerPhotoUrl(Guid listingId, Guid photoId) =>
        $"/api/market/listings/{listingId}/photos/{photoId}/content";

    internal static async Task<IResult> UploadAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        UploadListingPhotoUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(listingId, out Guid parsedListingId))
            return ToProblem(ListingPhotoErrors.InvalidIdentifier, httpContext);

        Result<ListingPhoto> result = await useCase.ExecuteAsync(
            parsedListingId,
            GetActor(actorAccessor),
            httpContext.Request.Body,
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error, httpContext);

        ListingPhoto photo = result.Value;

        return Results.Created(
            OwnerPhotoUrl(photo.ListingId, photo.Id),
            ToResponse(photo));
    }

    internal static async Task<IResult> ListAsync(
        string listingId,
        IAuthenticatedActorAccessor actorAccessor,
        ListListingPhotosUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(listingId, out Guid parsedListingId))
            return ToProblem(ListingPhotoErrors.InvalidIdentifier, httpContext);

        Result<IReadOnlyList<ListingPhoto>> result = await useCase.ExecuteAsync(
            parsedListingId,
            GetActor(actorAccessor),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new ListingPhotoCollectionResponse([.. result.Value.Select(ToResponse)]))
            : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> DeleteAsync(
        string listingId,
        string photoId,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteListingPhotoUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(listingId, out Guid parsedListingId) ||
            !TryParseId(photoId, out Guid parsedPhotoId))
        {
            return ToProblem(ListingPhotoErrors.InvalidIdentifier, httpContext);
        }

        Result result = await useCase.ExecuteAsync(
            parsedListingId,
            parsedPhotoId,
            GetActor(actorAccessor),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> GetOwnerContentAsync(
        string listingId,
        string photoId,
        IAuthenticatedActorAccessor actorAccessor,
        GetListingPhotoContentUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(listingId, out Guid parsedListingId) ||
            !TryParseId(photoId, out Guid parsedPhotoId))
        {
            return ToProblem(ListingPhotoErrors.InvalidIdentifier, httpContext);
        }

        Result<ListingPhotoContent> result = await useCase.ExecuteForOwnerAsync(
            parsedListingId,
            parsedPhotoId,
            GetActor(actorAccessor),
            cancellationToken);

        return result.IsSuccess
            ? Results.Stream(
                result.Value.Content,
                result.Value.Photo.Format.ToContentType())
            : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> GetPublicContentAsync(
        string listingId,
        string photoId,
        GetListingPhotoContentUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(listingId, out Guid parsedListingId) ||
            !TryParseId(photoId, out Guid parsedPhotoId))
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            return ToProblem(ListingPhotoErrors.InvalidIdentifier, httpContext);
        }

        Result<ListingPhotoContent> result = await useCase.ExecutePublicAsync(
            parsedListingId,
            parsedPhotoId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            return ToProblem(result.Error, httpContext);
        }

        // Photo content never changes for a given identifier, so it is cacheable forever.
        var entityTag = new EntityTagHeaderValue($"\"{parsedPhotoId:N}\"");
        httpContext.Response.Headers.CacheControl = PublicCacheControl;

        if (HttpMethods.IsHead(httpContext.Request.Method) ||
            RequestMatches(httpContext.Request, entityTag))
        {
            await result.Value.Content.DisposeAsync();
            httpContext.Response.Headers.ETag = entityTag.ToString();
            httpContext.Response.ContentType = result.Value.Photo.Format.ToContentType();

            return RequestMatches(httpContext.Request, entityTag)
                ? Results.StatusCode(StatusCodes.Status304NotModified)
                : Results.Ok();
        }

        return Results.Stream(
            result.Value.Content,
            result.Value.Photo.Format.ToContentType(),
            entityTag: entityTag);
    }

    internal static PublicListingPhotoResponse ToPublicResponse(ListingPhoto photo) =>
        new(photo.Id, PublicPhotoUrl(photo.ListingId, photo.Id));

    private static ListingPhotoResponse ToResponse(ListingPhoto photo) =>
        new(
            photo.Id,
            photo.Position,
            photo.Format.ToContentType(),
            photo.SizeBytes,
            photo.UploadedAt,
            OwnerPhotoUrl(photo.ListingId, photo.Id));

    private static bool RequestMatches(HttpRequest request, EntityTagHeaderValue entityTag) =>
        EntityTagHeaderValue.TryParseList(request.Headers.IfNoneMatch, out IList<EntityTagHeaderValue>? tags) &&
        tags.Any(tag => tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(entityTag, useStrongComparison: false));

    private static bool TryParseId(string value, out Guid id) =>
        Guid.TryParse(value, out id) && id != Guid.Empty;

    private static Guid GetActor(IAuthenticatedActorAccessor actorAccessor) =>
        actorAccessor.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");

    private static IResult ToProblem(Error error, HttpContext httpContext)
    {
        int statusCode =
            error == ListingPhotoErrors.TooLarge ? StatusCodes.Status413PayloadTooLarge
            : error == ListingPhotoErrors.UnsupportedFormat ? StatusCodes.Status415UnsupportedMediaType
            : error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => throw new InvalidOperationException(
                    $"Unsupported Listing photo error type '{error.Type}'."),
            };

        if (HttpMethods.IsHead(httpContext.Request.Method))
            return Results.StatusCode(statusCode);

        return Results.Problem(
            statusCode: statusCode,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier,
            });
    }

    internal sealed record ListingPhotoResponse(
        Guid PhotoId,
        int Position,
        string ContentType,
        long SizeBytes,
        DateTimeOffset UploadedAt,
        string Url);

    internal sealed record ListingPhotoCollectionResponse(
        IReadOnlyList<ListingPhotoResponse> Items);

    internal sealed record PublicListingPhotoResponse(
        Guid PhotoId,
        string Url);
}
