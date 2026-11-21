using Diyarak.Api.Authentication;
using Diyarak.Market.Application.OffPlanProjects;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

public static class MarketOffPlanProjectEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketOffPlanProjectEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/market/public/offplan", ListPublicAsync).AllowAnonymous();
        endpoints.MapGet("/api/market/public/offplan/{projectId}", GetPublicAsync).AllowAnonymous();

        endpoints.MapGet("/api/market/offplan", ListMineAsync).RequireMappedUserManagement();
        endpoints.MapPost("/api/market/offplan", CreateAsync).RequireMappedUserManagement();
        endpoints.MapPut("/api/market/offplan/{projectId}", UpdateAsync).RequireMappedUserManagement();
        endpoints.MapPut("/api/market/offplan/{projectId}/publish", TogglePublishAsync).RequireMappedUserManagement();
        endpoints.MapDelete("/api/market/offplan/{projectId}", DeleteAsync).RequireMappedUserManagement();

        return endpoints;
    }

    internal static async Task<IResult> ListPublicAsync(
        int? page,
        string? governorate,
        ListPublicOffPlanProjectsUseCase useCase,
        CancellationToken cancellationToken)
    {
        Result<PublicOffPlanProjectPage> result = await useCase.ExecuteAsync(
            page ?? 1, governorate, cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Ok(new
        {
            items = result.Value.Items.Select(item => ToPublicResponse(item.Project, item.Agency)),
            hasMore = result.Value.HasMore
        });
    }

    internal static async Task<IResult> GetPublicAsync(
        Guid projectId,
        GetPublicOffPlanProjectUseCase useCase,
        CancellationToken cancellationToken)
    {
        Result<PublicOffPlanProjectItem> result = await useCase.ExecuteAsync(projectId, cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Ok(ToPublicResponse(result.Value.Project, result.Value.Agency));
    }

    internal static async Task<IResult> ListMineAsync(
        IAuthenticatedActorAccessor actorAccessor,
        ListMyOffPlanProjectsUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        Result<IReadOnlyList<OffPlanProject>> result = await useCase.ExecuteAsync(actorUserId, cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Ok(new { items = result.Value.Select(ToManagementResponse) });
    }

    internal static async Task<IResult> CreateAsync(
        CreateProjectRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        CreateOffPlanProjectUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        Result<OffPlanProject> result = await useCase.ExecuteAsync(
            actorUserId,
            new SaveOffPlanProjectCommand(
                request.Name,
                request.Description,
                request.Governorate,
                request.District,
                request.ConstructionStatus,
                request.TotalUnits,
                request.MinPriceUsd,
                request.MaxPriceUsd,
                request.ExpectedCompletion),
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Created($"/api/market/offplan/{result.Value.Id}", ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> UpdateAsync(
        Guid projectId,
        UpdateProjectRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        UpdateOffPlanProjectUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        Result<OffPlanProject> result = await useCase.ExecuteAsync(
            actorUserId,
            projectId,
            request.ExpectedVersion,
            new SaveOffPlanProjectCommand(
                request.Name,
                request.Description,
                request.Governorate,
                request.District,
                request.ConstructionStatus,
                request.TotalUnits,
                request.MinPriceUsd,
                request.MaxPriceUsd,
                request.ExpectedCompletion),
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Ok(ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> TogglePublishAsync(
        Guid projectId,
        TogglePublishRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        PublishOffPlanProjectUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        Result<OffPlanProject> result = await useCase.ExecuteAsync(
            actorUserId, projectId, request.ExpectedVersion, request.Publish, cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error);

        return Results.Ok(ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> DeleteAsync(
        Guid projectId,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteOffPlanProjectUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        Result deleteResult = await useCase.ExecuteAsync(actorUserId, projectId, cancellationToken);

        if (!deleteResult.IsSuccess)
            return ToProblem(deleteResult.Error);

        return Results.NoContent();
    }

    private static object ToPublicResponse(OffPlanProject project, Agency agency) => new
    {
        id = project.Id,
        name = project.Name,
        description = project.Description,
        governorate = project.Governorate,
        district = project.District,
        constructionStatus = project.ConstructionStatus.ToString(),
        totalUnits = project.TotalUnits,
        minPriceUsd = project.MinPriceUsd,
        maxPriceUsd = project.MaxPriceUsd,
        expectedCompletion = project.ExpectedCompletion?.ToString("yyyy-MM-dd"),
        agency = new
        {
            id = agency.Id,
            name = agency.Name,
            isVerified = agency.IsVerified,
        },
    };

    private static object ToManagementResponse(OffPlanProject project) => new
    {
        id = project.Id,
        version = project.Version,
        name = project.Name,
        description = project.Description,
        governorate = project.Governorate,
        district = project.District,
        constructionStatus = project.ConstructionStatus.ToString(),
        totalUnits = project.TotalUnits,
        minPriceUsd = project.MinPriceUsd,
        maxPriceUsd = project.MaxPriceUsd,
        expectedCompletion = project.ExpectedCompletion?.ToString("yyyy-MM-dd"),
        isPublished = project.IsPublished,
        createdAt = project.CreatedAt,
    };

    private static Guid GetRequiredActorUserId(IAuthenticatedActorAccessor actorAccessor)
    {
        ArgumentNullException.ThrowIfNull(actorAccessor);
        Guid? actorUserId = actorAccessor.UserId;
        if (actorUserId is null || actorUserId == Guid.Empty)
            throw new InvalidOperationException("Authenticated actor user identifier is required.");
        return actorUserId.Value;
    }

    private static IResult ToProblem(Error error) => error.Type switch
    {
        ErrorType.Validation => Results.BadRequest(new { error = error.Description }),
        ErrorType.NotFound => Results.NotFound(new { error = error.Description }),
        ErrorType.Conflict => Results.Conflict(new { error = error.Description }),
        _ => Results.Problem(error.Description, statusCode: 500),
    };

    internal sealed record CreateProjectRequest(
        string Name,
        string? Description,
        string Governorate,
        string District,
        ConstructionStatus ConstructionStatus,
        int TotalUnits,
        decimal? MinPriceUsd,
        decimal? MaxPriceUsd,
        DateOnly? ExpectedCompletion);

    internal sealed record UpdateProjectRequest(
        long ExpectedVersion,
        string Name,
        string? Description,
        string Governorate,
        string District,
        ConstructionStatus ConstructionStatus,
        int TotalUnits,
        decimal? MinPriceUsd,
        decimal? MaxPriceUsd,
        DateOnly? ExpectedCompletion);

    internal sealed record TogglePublishRequest(long ExpectedVersion, bool Publish);
}
