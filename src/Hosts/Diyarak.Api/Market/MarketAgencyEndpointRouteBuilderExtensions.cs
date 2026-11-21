using System.Globalization;
using Diyarak.Api.Authentication;
using Diyarak.Api.Moderation;
using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using AgencyPackage = Diyarak.Market.Company.AgencyPackage;

namespace Diyarak.Api.Market;

/// <summary>Agency profiles, their public pages and moderator verification (ADR-0068).</summary>
public static class MarketAgencyEndpointRouteBuilderExtensions
{
    /// <summary>The public agency page, mapped regardless of authentication.</summary>
    public static IEndpointRouteBuilder MapPublicAgencyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/market/public/agencies/{agencyId}", GetPublicAsync).AllowAnonymous();
        return endpoints;
    }

    public static IEndpointRouteBuilder MapMarketAgencyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/market/agency", GetMineAsync).RequireMappedUserManagement();
        endpoints.MapPut("/api/market/agency", SaveMineAsync).RequireMappedUserManagement();
        endpoints.MapPost("/api/market/agency/verification", RequestVerificationAsync).RequireMappedUserManagement();

        RouteGroupBuilder moderation = endpoints.MapGroup("/api/moderation/agencies");
        moderation.RequireAuthorization(ModerationEndpointRouteBuilderExtensions.ModeratorPolicy);
        moderation.WithMetadata(MarketManagementEndpointMetadata.Instance);
        moderation.MapGet(string.Empty, ListPendingAsync);
        moderation.MapPost("/{agencyId}/verify", (string agencyId, DecideAgencyVerificationUseCase useCase, HttpContext http, CancellationToken token) =>
            DecideAsync(agencyId, AgencyVerificationDecision.Verify, null, useCase, http, token));
        moderation.MapPost("/{agencyId}/reject", (string agencyId, RejectAgencyRequest? request, DecideAgencyVerificationUseCase useCase, HttpContext http, CancellationToken token) =>
            DecideAsync(agencyId, AgencyVerificationDecision.Reject, request?.Note, useCase, http, token));

        moderation.MapPost("/{agencyId}/packages", AssignPackageAsync);
        moderation.MapDelete("/{agencyId}/packages/{packageId}", RevokePackageAsync);
        moderation.MapGet("/{agencyId}/packages", ListPackagesAsync);

        endpoints.MapGet("/api/market/quota", GetQuotaAsync).RequireMappedUserManagement();

        return endpoints;
    }

    internal static async Task<IResult> GetMineAsync(IAuthenticatedActorAccessor actor, GetMyAgencyUseCase useCase, HttpContext http, CancellationToken token)
    {
        Result<Agency> result = await useCase.ExecuteAsync(RequireActor(actor), token);
        http.Response.Headers.CacheControl = "no-store";
        return result.IsSuccess ? Results.Ok(ToOwnerResponse(result.Value)) : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> SaveMineAsync(SaveAgencyRequest? request, IAuthenticatedActorAccessor actor, SaveMyAgencyUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (request is null || request.Name is null || request.City is null)
            return ToProblem(AgencyErrors.InvalidRequest, http);

        Result<Agency> result = await useCase.ExecuteAsync(
            RequireActor(actor),
            new SaveAgencyCommand(request.Name, request.Description, request.Governorate, request.City, request.Version),
            token);

        return result.IsSuccess ? Results.Ok(ToOwnerResponse(result.Value)) : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> RequestVerificationAsync(VerificationRequest? request, IAuthenticatedActorAccessor actor, RequestAgencyVerificationUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (request is null || request.RegistrationNumber is null || request.Version is not > 0)
            return ToProblem(AgencyErrors.InvalidRequest, http);

        Result<Agency> result = await useCase.ExecuteAsync(RequireActor(actor), request.RegistrationNumber, request.Version.Value, token);
        return result.IsSuccess ? Results.Ok(ToOwnerResponse(result.Value)) : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> GetPublicAsync(string agencyId, GetPublicAgencyUseCase useCase, HttpContext http, CancellationToken token)
    {
        string? pageValue = http.Request.Query["page"];
        int page = 1;
        if (!Guid.TryParse(agencyId, out Guid parsed) ||
            (!string.IsNullOrEmpty(pageValue) && !int.TryParse(pageValue, NumberStyles.None, CultureInfo.InvariantCulture, out page)))
        {
            return ToProblem(AgencyErrors.InvalidRequest, http);
        }

        Result<PublicAgencyPage> result = await useCase.ExecuteAsync(parsed, page, token);
        if (!result.IsSuccess)
            return ToProblem(result.Error, http);

        Agency agency = result.Value.Agency;
        http.Response.Headers.CacheControl = "public, max-age=0, must-revalidate";

        return Results.Ok(new PublicAgencyResponse(
            new PublicAgencyProfileResponse(agency.Id, agency.Name, agency.Description, agency.Governorate, agency.City, agency.IsVerified, agency.VerifiedAt),
            [.. result.Value.Listings.Select(item => PublicMarketListingEndpointRouteBuilderExtensions.ToResponse(item, agency))],
            page,
            result.Value.HasMore));
    }

    internal static async Task<IResult> ListPendingAsync(IAgencyRepository agencies, HttpContext http, CancellationToken token)
    {
        IReadOnlyList<Agency> pending = await agencies.ListByStatusAsync(AgencyVerificationStatus.Pending, 100, token);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new PendingAgenciesResponse([.. pending.Select(ToOwnerResponse)]));
    }

    internal static async Task<IResult> DecideAsync(string agencyId, AgencyVerificationDecision decision, string? note, DecideAgencyVerificationUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(agencyId, out Guid parsed))
            return ToProblem(AgencyErrors.InvalidRequest, http);

        Result<Agency> result = await useCase.ExecuteAsync(parsed, decision, note, token);
        return result.IsSuccess ? Results.Ok(ToOwnerResponse(result.Value)) : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> GetQuotaAsync(IAuthenticatedActorAccessor actor, GetListingQuotaUseCase useCase, HttpContext http, CancellationToken token)
    {
        ListingQuotaInfo info = await useCase.ExecuteAsync(RequireActor(actor), token);
        http.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new QuotaResponse(
            info.Quota,
            info.ActiveListings,
            info.Remaining,
            info.ActivePackage is { } package
                ? new PackageResponse(package.Id, package.ListingQuota, package.ExpiresAt, package.Note, package.CreatedAt)
                : null));
    }

    internal static async Task<IResult> AssignPackageAsync(
        string agencyId, AssignPackageRequest? request,
        AssignAgencyPackageUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(agencyId, out Guid parsed) || request is null || request.ListingQuota is not > 0 || request.DurationDays is not > 0)
            return ToProblem(AgencyPackageErrors.InvalidRequest, http);

        Result<AgencyPackage> result = await useCase.ExecuteAsync(parsed, request.ListingQuota.Value, request.DurationDays.Value, request.Note, token);
        if (!result.IsSuccess)
            return ToProblem(result.Error, http);

        AgencyPackage package = result.Value;
        return Results.Created(
            $"/api/moderation/agencies/{agencyId}/packages",
            new PackageResponse(package.Id, package.ListingQuota, package.ExpiresAt, package.Note, package.CreatedAt));
    }

    internal static async Task<IResult> RevokePackageAsync(
        string agencyId, string packageId,
        RevokeAgencyPackageUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(agencyId, out _) || !Guid.TryParse(packageId, out Guid parsedPackageId))
            return ToProblem(AgencyPackageErrors.InvalidRequest, http);

        Result result = await useCase.ExecuteAsync(parsedPackageId, token);
        return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> ListPackagesAsync(
        string agencyId,
        IAgencyPackageRepository packages, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(agencyId, out Guid parsed))
            return ToProblem(AgencyPackageErrors.InvalidRequest, http);

        IReadOnlyList<AgencyPackage> all = await packages.ListByAgencyAsync(parsed, token);
        http.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new PackageListResponse([.. all.Select(p =>
            new PackageDetailResponse(p.Id, p.ListingQuota, p.ExpiresAt, p.Note, p.CreatedAt, p.IsRevoked, p.RevokedAt))]));
    }

    private static OwnerAgencyResponse ToOwnerResponse(Agency agency) =>
        new(
            agency.Id,
            agency.Version,
            agency.Name,
            agency.Description,
            agency.Governorate,
            agency.City,
            agency.VerificationStatus.ToString(),
            agency.RegistrationNumber,
            agency.VerificationNote,
            agency.VerifiedAt);

    private static Guid RequireActor(IAuthenticatedActorAccessor actor) =>
        actor.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException(
                "The mapped-user authorization policy succeeded without supplying a non-empty internal user identifier.");

    private static IResult ToProblem(Error error, HttpContext http)
    {
        (int statusCode, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => throw new InvalidOperationException($"Unsupported expected agency error type '{error.Type}'."),
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = http.TraceIdentifier });
    }

    internal sealed record SaveAgencyRequest(string? Name, string? Description, string? Governorate, string? City, long? Version);

    internal sealed record VerificationRequest(string? RegistrationNumber, long? Version);

    internal sealed record RejectAgencyRequest(string? Note);

    /// <summary>The owner's and moderators' view; includes the registration number.</summary>
    internal sealed record OwnerAgencyResponse(
        Guid AgencyId,
        long Version,
        string Name,
        string? Description,
        string? Governorate,
        string City,
        string VerificationStatus,
        string? RegistrationNumber,
        string? VerificationNote,
        DateTimeOffset? VerifiedAt);

    internal sealed record PendingAgenciesResponse(IReadOnlyList<OwnerAgencyResponse> Items);

    /// <summary>The public profile; never includes the owner or the registration number.</summary>
    internal sealed record PublicAgencyProfileResponse(
        Guid AgencyId,
        string Name,
        string? Description,
        string? Governorate,
        string City,
        bool Verified,
        DateTimeOffset? VerifiedAt);

    internal sealed record PublicAgencyResponse(
        PublicAgencyProfileResponse Agency,
        IReadOnlyList<PublicMarketListingEndpointRouteBuilderExtensions.PublicMarketListingResponse> Items,
        int Page,
        bool HasMore);

    internal sealed record AssignPackageRequest(int? ListingQuota, int? DurationDays, string? Note);

    internal sealed record PackageResponse(Guid PackageId, int ListingQuota, DateTimeOffset ExpiresAt, string? Note, DateTimeOffset CreatedAt);

    internal sealed record PackageDetailResponse(
        Guid PackageId, int ListingQuota, DateTimeOffset ExpiresAt, string? Note,
        DateTimeOffset CreatedAt, bool IsRevoked, DateTimeOffset? RevokedAt);

    internal sealed record PackageListResponse(IReadOnlyList<PackageDetailResponse> Items);

    internal sealed record QuotaResponse(int Quota, int ActiveListings, int Remaining, PackageResponse? ActivePackage);
}
