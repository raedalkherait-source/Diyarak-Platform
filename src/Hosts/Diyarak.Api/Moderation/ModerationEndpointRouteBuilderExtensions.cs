using Diyarak.Api.Authentication;
using Diyarak.Api.Market;
using Diyarak.Api.RateLimiting;
using Diyarak.Market.Application.Moderation;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Microsoft.AspNetCore.Authorization;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Moderation;

internal static class ModerationEndpointRouteBuilderExtensions
{
    internal const string ModeratorPolicy = "moderator";

    internal static IServiceCollection AddModeration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(ModeratorOptions.Load(configuration));
        services.AddScoped<IAuthorizationHandler, ModeratorAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(
                ModeratorPolicy,
                policy => policy.RequireAuthenticatedUser().AddRequirements(new ModeratorRequirement()));

        services.AddScoped<ReportListingUseCase>();
        services.AddScoped<ListModerationQueueUseCase>();
        services.AddScoped<ListSuspendedListingsUseCase>();
        services.AddScoped<GetListingForModerationUseCase>();
        services.AddScoped<ModerateListingUseCase>();
        return services;
    }

    /// <summary>Anonymous reporting of a public listing; mapped regardless of authentication.</summary>
    internal static IEndpointRouteBuilder MapPublicListingReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/api/market/public/listings/{listingId}/reports", ReportAsync)
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.ReportPolicy);

        return endpoints;
    }

    internal static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/moderation");
        group.RequireAuthorization(ModeratorPolicy);
        group.WithMetadata(MarketManagementEndpointMetadata.Instance);

        group.MapGet("/queue", QueueAsync);
        group.MapGet("/suspended", SuspendedAsync);
        group.MapGet("/listings/{listingId}", GetAsync);
        group.MapPost("/listings/{listingId}/suspend", (string listingId, SuspendRequest? request, IAuthenticatedActorAccessor actor, ModerateListingUseCase useCase, HttpContext http, CancellationToken token) =>
            ModerateAsync(listingId, ModerationAction.Suspend, request?.Note, actor, useCase, http, token));
        group.MapPost("/listings/{listingId}/reinstate", (string listingId, IAuthenticatedActorAccessor actor, ModerateListingUseCase useCase, HttpContext http, CancellationToken token) =>
            ModerateAsync(listingId, ModerationAction.Reinstate, null, actor, useCase, http, token));
        group.MapPost("/listings/{listingId}/dismiss-reports", (string listingId, IAuthenticatedActorAccessor actor, ModerateListingUseCase useCase, HttpContext http, CancellationToken token) =>
            ModerateAsync(listingId, ModerationAction.DismissReports, null, actor, useCase, http, token));

        return endpoints;
    }

    internal static async Task<IResult> ReportAsync(
        string listingId,
        ReportRequest? request,
        ReportListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        if (!Guid.TryParse(listingId, out Guid parsedListingId) ||
            request is null ||
            !Enum.TryParse(request.Reason, ignoreCase: false, out ListingReportReason reason) ||
            !Enum.IsDefined(reason))
        {
            return ToProblem(ModerationErrors.InvalidRequest, httpContext);
        }

        Result<Guid> result = await useCase.ExecuteAsync(parsedListingId, reason, request.Comment, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted(value: new ReportResponse(result.Value))
            : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> QueueAsync(ListModerationQueueUseCase useCase, CancellationToken cancellationToken)
    {
        IReadOnlyList<ModerationQueueItem> items = await useCase.ExecuteAsync(cancellationToken);
        return Results.Ok(new ModerationQueueResponse([.. items.Select(ToItemResponse)]));
    }

    internal static async Task<IResult> SuspendedAsync(ListSuspendedListingsUseCase useCase, CancellationToken cancellationToken)
    {
        IReadOnlyList<MarketListing> listings = await useCase.ExecuteAsync(cancellationToken);
        return Results.Ok(new ModerationListingCollectionResponse([.. listings.Select(ToListingResponse)]));
    }

    internal static async Task<IResult> GetAsync(
        string listingId,
        GetListingForModerationUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId))
            return ToProblem(ModerationErrors.ListingNotFound, httpContext);

        Result<ModerationQueueItem> result = await useCase.ExecuteAsync(parsedListingId, cancellationToken);
        return result.IsSuccess ? Results.Ok(ToItemResponse(result.Value)) : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> ModerateAsync(
        string listingId,
        ModerationAction action,
        string? note,
        IAuthenticatedActorAccessor actorAccessor,
        ModerateListingUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(listingId, out Guid parsedListingId) || actorAccessor.UserId is not { } moderator)
            return ToProblem(ModerationErrors.InvalidRequest, httpContext);

        Result result = await useCase.ExecuteAsync(parsedListingId, moderator, action, note, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error, httpContext);
    }

    private static ModerationItemResponse ToItemResponse(ModerationQueueItem item) =>
        new(
            ToListingResponse(item.Listing),
            [.. item.OpenReports.Select(report => new ModerationReportResponse(report.Id, report.Reason.ToString(), report.Comment, report.CreatedAt))]);

    private static ModerationListingResponse ToListingResponse(MarketListing listing) =>
        new(
            listing.Id,
            listing.Status.ToString(),
            listing.PublisherUserId,
            listing.Headline?.Value,
            listing.Context?.TransactionIntent.ToString(),
            listing.Price is { } price ? new MarketListingEndpointRouteBuilderExtensions.MarketListingPriceResponse(price.IsOnRequest, price.Amount?.Amount, price.Amount?.Currency.Code) : null,
            listing.ModerationNote,
            listing.ClosureReason?.ToString());

    private static IResult ToProblem(Error error, HttpContext httpContext)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict when error == ModerationErrors.TooManyReports => StatusCodes.Status429TooManyRequests,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new InvalidOperationException($"Unsupported moderation error type '{error.Type}'."),
        };

        return Results.Problem(
            statusCode: statusCode,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = httpContext.TraceIdentifier });
    }

    internal sealed record ReportRequest(string? Reason, string? Comment);

    internal sealed record ReportResponse(Guid ReportId);

    internal sealed record SuspendRequest(string? Note);

    internal sealed record ModerationReportResponse(Guid ReportId, string Reason, string? Comment, DateTimeOffset CreatedAt);

    internal sealed record ModerationListingResponse(
        Guid ListingId,
        string Status,
        Guid PublisherUserId,
        string? Headline,
        string? TransactionIntent,
        MarketListingEndpointRouteBuilderExtensions.MarketListingPriceResponse? Price,
        string? ModerationNote,
        string? ClosureReason);

    internal sealed record ModerationItemResponse(ModerationListingResponse Listing, IReadOnlyList<ModerationReportResponse> OpenReports);

    internal sealed record ModerationQueueResponse(IReadOnlyList<ModerationItemResponse> Items);

    internal sealed record ModerationListingCollectionResponse(IReadOnlyList<ModerationListingResponse> Items);
}
