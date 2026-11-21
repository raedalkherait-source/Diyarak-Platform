using Diyarak.Api.Authentication;
using Diyarak.Market.Application.BulkImport;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

public static class MarketBulkImportEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketBulkImportEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/api/market/import/csv", ImportCsvAsync)
            .RequireMappedUserManagement()
            .DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> ImportCsvAsync(
        IFormFile file,
        IAuthenticatedActorAccessor actorAccessor,
        BulkImportListingsUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "A CSV file is required." });

        if (file.Length > 2 * 1024 * 1024)
            return Results.BadRequest(new { error = "File size must not exceed 2 MB." });

        using var reader = new StreamReader(file.OpenReadStream());
        string csvContent = await reader.ReadToEndAsync(cancellationToken);

        Result<IReadOnlyList<BulkImportRow>> parseResult = CsvParser.Parse(csvContent);
        if (!parseResult.IsSuccess)
            return Results.BadRequest(new { error = parseResult.Error.Description });

        Result<BulkImportResult> result = await useCase.ExecuteAsync(
            actorUserId, parseResult.Value, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => Results.BadRequest(new { error = result.Error.Description }),
                _ => Results.Problem(result.Error.Description, statusCode: 500)
            };
        }

        return Results.Ok(new
        {
            created = result.Value.Created,
            failed = result.Value.Failed,
            errors = result.Value.Errors
        });
    }

    private static Guid GetRequiredActorUserId(
        IAuthenticatedActorAccessor actorAccessor)
    {
        ArgumentNullException.ThrowIfNull(actorAccessor);
        Guid? actorUserId = actorAccessor.UserId;
        if (actorUserId is null || actorUserId == Guid.Empty)
            throw new InvalidOperationException("Authenticated actor user identifier is required.");
        return actorUserId.Value;
    }
}
