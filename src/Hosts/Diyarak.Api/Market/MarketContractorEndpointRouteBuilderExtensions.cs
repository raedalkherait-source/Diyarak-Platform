using Diyarak.Api.Authentication;
using Diyarak.Market.Application.Contractors;
using Diyarak.Market.Company;

namespace Diyarak.Api.Market;

public static class MarketContractorEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketContractorEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/market/public/contractors", ListPublicAsync).AllowAnonymous();

        endpoints.MapGet("/api/market/contractor", GetMineAsync).RequireMappedUserManagement();
        endpoints.MapPost("/api/market/contractor", CreateAsync).RequireMappedUserManagement();
        endpoints.MapPut("/api/market/contractor/{contractorId}", UpdateAsync).RequireMappedUserManagement();
        endpoints.MapPut("/api/market/contractor/{contractorId}/publish", TogglePublishAsync).RequireMappedUserManagement();
        endpoints.MapDelete("/api/market/contractor/{contractorId}", DeleteAsync).RequireMappedUserManagement();

        return endpoints;
    }

    internal static async Task<IResult> ListPublicAsync(
        int? page,
        string? governorate,
        int? specialty,
        ListPublicContractorsUseCase useCase,
        CancellationToken cancellationToken)
    {
        ContractorSpecialty? parsedSpecialty = specialty.HasValue && Enum.IsDefined((ContractorSpecialty)specialty.Value)
            ? (ContractorSpecialty)specialty.Value
            : null;

        var result = await useCase.ExecuteAsync(page ?? 1, governorate, parsedSpecialty, cancellationToken);
        if (!result.IsSuccess) return Results.Problem(statusCode: 500);

        const int pageSize = 20;
        var items = result.Value;
        bool hasMore = items.Count > pageSize;
        var pageItems = hasMore ? items.Take(pageSize).ToArray() : items;

        return Results.Ok(new
        {
            items = pageItems.Select(ToPublicResponse).ToArray(),
            hasMore,
        });
    }

    internal static async Task<IResult> GetMineAsync(
        IAuthenticatedActorAccessor actorAccessor,
        GetMyContractorUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var result = await useCase.ExecuteAsync(actorUserId, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.Ok(ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> CreateAsync(
        CreateContractorRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        CreateContractorUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContractorSpecialty>(request.Specialty, ignoreCase: true, out var specialty))
            return Results.BadRequest(new { error = "Invalid specialty." });

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var command = new SaveContractorCommand(
            request.Name, request.Description,
            request.Governorate, request.District,
            specialty, request.Phone);

        var result = await useCase.ExecuteAsync(actorUserId, command, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.Created($"/api/market/contractor/{result.Value.Id}", ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> UpdateAsync(
        Guid contractorId,
        UpdateContractorRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        UpdateContractorUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContractorSpecialty>(request.Specialty, ignoreCase: true, out var specialty))
            return Results.BadRequest(new { error = "Invalid specialty." });

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var command = new SaveContractorCommand(
            request.Name, request.Description,
            request.Governorate, request.District,
            specialty, request.Phone);

        var result = await useCase.ExecuteAsync(actorUserId, contractorId, request.Version, command, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.Ok(ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> TogglePublishAsync(
        Guid contractorId,
        ToggleContractorPublishRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        ToggleContractorPublishUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var result = await useCase.ExecuteAsync(actorUserId, contractorId, request.Version, request.Publish, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.Ok(ToManagementResponse(result.Value));
    }

    internal static async Task<IResult> DeleteAsync(
        Guid contractorId,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteContractorUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var result = await useCase.ExecuteAsync(actorUserId, contractorId, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.NoContent();
    }

    private static object ToPublicResponse(Contractor c) => new
    {
        id = c.Id,
        name = c.Name,
        description = c.Description,
        governorate = c.Governorate,
        district = c.District,
        specialty = c.Specialty.ToString(),
        phone = c.Phone,
    };

    private static object ToManagementResponse(Contractor c) => new
    {
        id = c.Id,
        name = c.Name,
        description = c.Description,
        governorate = c.Governorate,
        district = c.District,
        specialty = c.Specialty.ToString(),
        phone = c.Phone,
        isPublished = c.IsPublished,
        version = c.Version,
    };

    private static Guid GetRequiredActorUserId(IAuthenticatedActorAccessor actorAccessor)
    {
        ArgumentNullException.ThrowIfNull(actorAccessor);
        Guid? actorUserId = actorAccessor.UserId;
        if (actorUserId is null || actorUserId == Guid.Empty)
            throw new InvalidOperationException("Authenticated actor user identifier is required.");
        return actorUserId.Value;
    }

    private static IResult ToProblem(Diyarak.Platform.BuildingBlocks.Error error) => error.Type switch
    {
        Diyarak.Platform.BuildingBlocks.ErrorType.Validation => Results.BadRequest(new { error = error.Description }),
        Diyarak.Platform.BuildingBlocks.ErrorType.NotFound => Results.NotFound(new { error = error.Description }),
        Diyarak.Platform.BuildingBlocks.ErrorType.Conflict => Results.Conflict(new { error = error.Description }),
        _ => Results.Problem(error.Description, statusCode: 500),
    };

    internal sealed record CreateContractorRequest(
        string Name,
        string? Description,
        string Governorate,
        string? District,
        string Specialty,
        string Phone);

    internal sealed record UpdateContractorRequest(
        string Name,
        string? Description,
        string Governorate,
        string? District,
        string Specialty,
        string Phone,
        long Version);

    internal sealed record ToggleContractorPublishRequest(long Version, bool Publish);
}
