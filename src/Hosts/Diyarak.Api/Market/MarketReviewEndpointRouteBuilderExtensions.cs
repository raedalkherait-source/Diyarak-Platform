using Diyarak.Api.Authentication;
using Diyarak.Market.Application.Reviews;
using Diyarak.Market.Company;

namespace Diyarak.Api.Market;

public static class MarketReviewEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketReviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/market/public/reviews", ListReviewsAsync).AllowAnonymous();
        endpoints.MapGet("/api/market/public/reviews/rating", GetRatingAsync).AllowAnonymous();

        endpoints.MapPost("/api/market/reviews", CreateAsync).RequireMappedUserManagement();
        endpoints.MapDelete("/api/market/reviews/{reviewId}", DeleteAsync).RequireMappedUserManagement();

        return endpoints;
    }

    internal static async Task<IResult> ListReviewsAsync(
        Guid subjectId,
        int? page,
        ListSubjectReviewsUseCase useCase,
        CancellationToken cancellationToken)
    {
        int currentPage = Math.Max(1, page ?? 1);
        var result = await useCase.ExecuteAsync(subjectId, currentPage, cancellationToken);
        if (!result.IsSuccess) return Results.Problem(statusCode: 500);

        const int pageSize = 20;
        var items = result.Value;
        bool hasMore = items.Count > pageSize;
        var pageItems = hasMore ? items.Take(pageSize).ToArray() : items;

        return Results.Ok(new
        {
            items = pageItems.Select(ToReviewResponse).ToArray(),
            hasMore,
        });
    }

    internal static async Task<IResult> GetRatingAsync(
        Guid subjectId,
        GetSubjectRatingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(subjectId, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);

        return Results.Ok(new { average = result.Value.Average, count = result.Value.Count });
    }

    internal static async Task<IResult> CreateAsync(
        CreateReviewRequest request,
        IAuthenticatedActorAccessor actorAccessor,
        CreateReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ReviewSubjectType>(request.SubjectType, ignoreCase: true, out var subjectType))
            return Results.BadRequest(new { error = "Invalid subject type." });

        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var command = new CreateReviewCommand(request.SubjectId, subjectType, request.Rating, request.Comment);

        var result = await useCase.ExecuteAsync(actorUserId, command, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);

        return Results.Created($"/api/market/public/reviews?subjectId={result.Value.SubjectId}", new
        {
            id = result.Value.Id,
            rating = result.Value.Rating,
            comment = result.Value.Comment,
            createdAt = result.Value.CreatedAt,
        });
    }

    internal static async Task<IResult> DeleteAsync(
        Guid reviewId,
        IAuthenticatedActorAccessor actorAccessor,
        DeleteReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid actorUserId = GetRequiredActorUserId(actorAccessor);
        var result = await useCase.ExecuteAsync(actorUserId, reviewId, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error);
        return Results.NoContent();
    }

    private static object ToReviewResponse(Review r) => new
    {
        id = r.Id,
        authorUserId = r.AuthorUserId,
        rating = r.Rating,
        comment = r.Comment,
        createdAt = r.CreatedAt,
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

    internal sealed record CreateReviewRequest(Guid SubjectId, string SubjectType, int Rating, string? Comment);
}
