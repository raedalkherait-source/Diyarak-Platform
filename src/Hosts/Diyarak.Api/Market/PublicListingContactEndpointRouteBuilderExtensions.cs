using Diyarak.Api.RateLimiting;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

internal static class PublicListingContactEndpointRouteBuilderExtensions
{
    internal const string ContactPath = "/api/market/public/listings/{listingId}/contact";

    internal static IEndpointRouteBuilder MapPublicListingContactEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapGet(ContactPath, GetContactAsync)
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.ContactPolicy);

        return endpoints;
    }

    internal static async Task<IResult> GetContactAsync(
        string listingId,
        GetListingContactUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Phone numbers must not be cached by shared caches or indexed by search engines.
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        if (!Guid.TryParse(listingId, out Guid parsedListingId))
            return ToProblem(GetListingContactErrors.NotFound, httpContext);

        Result<ListingContactDetails> result =
            await useCase.ExecuteAsync(parsedListingId, cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error, httpContext);

        ListingContactDetails contact = result.Value;
        string digits = contact.PhoneNumber.Value.TrimStart('+');

        return Results.Ok(
            new PublicListingContactResponse(
                contact.PhoneNumber.Value,
                contact.AllowCalls ? $"tel:{contact.PhoneNumber.Value}" : null,
                contact.AllowWhatsApp ? $"https://wa.me/{digits}" : null));
    }

    private static IResult ToProblem(Error error, HttpContext httpContext) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier,
            });

    internal sealed record PublicListingContactResponse(
        string PhoneNumber,
        string? CallUrl,
        string? WhatsAppUrl);
}
