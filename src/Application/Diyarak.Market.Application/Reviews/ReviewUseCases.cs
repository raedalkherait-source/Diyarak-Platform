using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Application.Contractors;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Reviews;

public interface IReviewRepository
{
    public Task<Review?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Review?> FindByAuthorAndSubjectAsync(Guid authorUserId, Guid subjectId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Review>> ListBySubjectAsync(Guid subjectId, int offset, int take, CancellationToken cancellationToken = default);

    public Task<(double Average, int Count)?> GetSubjectAverageAsync(Guid subjectId, CancellationToken cancellationToken = default);

    public Task AddAsync(Review review, CancellationToken cancellationToken = default);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public static class ReviewErrors
{
    public static readonly Error InvalidRequest = Error.Validation("market.review.invalid_request", "The review details are invalid.");
    public static readonly Error NotFound = Error.NotFound("market.review.not_found", "Review not found.");
    public static readonly Error AlreadyReviewed = Error.Conflict("market.review.already_reviewed", "لقد قمت بتقييم هذا المقاول/المكتب مسبقاً.");
    public static readonly Error CannotReviewOwnProfile = Error.Validation("market.review.cannot_review_own", "لا يمكنك تقييم ملفك الشخصي.");
    public static readonly Error SubjectNotFound = Error.NotFound("market.review.subject_not_found", "The contractor or agency was not found.");
}

public sealed record CreateReviewCommand(Guid SubjectId, ReviewSubjectType SubjectType, int Rating, string? Comment);

public sealed record SubjectRating(double Average, int Count);

public sealed class CreateReviewUseCase(
    IReviewRepository reviews,
    IContractorRepository contractors,
    IAgencyRepository agencies,
    IClock clock)
{
    public async Task<Result<Review>> ExecuteAsync(
        Guid authorUserId,
        CreateReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        Guid subjectOwnerUserId;

        if (command.SubjectType == ReviewSubjectType.Contractor)
        {
            var contractor = await contractors.FindByIdAsync(command.SubjectId, cancellationToken);
            if (contractor is null || !contractor.IsPublished)
                return Result.Failure<Review>(ReviewErrors.SubjectNotFound);
            subjectOwnerUserId = contractor.OwnerUserId;
        }
        else if (command.SubjectType == ReviewSubjectType.Agency)
        {
            var agency = await agencies.FindByIdAsync(command.SubjectId, cancellationToken);
            if (agency is null)
                return Result.Failure<Review>(ReviewErrors.SubjectNotFound);
            subjectOwnerUserId = agency.OwnerUserId;
        }
        else
        {
            return Result.Failure<Review>(ReviewErrors.InvalidRequest);
        }

        if (authorUserId == subjectOwnerUserId)
            return Result.Failure<Review>(ReviewErrors.CannotReviewOwnProfile);

        var existing = await reviews.FindByAuthorAndSubjectAsync(authorUserId, command.SubjectId, cancellationToken);
        if (existing is not null)
            return Result.Failure<Review>(ReviewErrors.AlreadyReviewed);

        Review review;
        try
        {
            review = Review.Create(
                Guid.NewGuid(), authorUserId,
                command.SubjectId, command.SubjectType,
                command.Rating, command.Comment,
                clock.UtcNow);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Review>(ReviewErrors.InvalidRequest);
        }

        await reviews.AddAsync(review, cancellationToken);
        return Result<Review>.Success(review);
    }
}

public sealed class DeleteReviewUseCase(IReviewRepository reviews)
{
    public async Task<Result> ExecuteAsync(
        Guid authorUserId,
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await reviews.FindByIdAsync(reviewId, cancellationToken);
        if (review is null || review.AuthorUserId != authorUserId)
            return Result.Failure(ReviewErrors.NotFound);

        await reviews.DeleteAsync(reviewId, cancellationToken);
        return Result.Success();
    }
}

public sealed class ListSubjectReviewsUseCase(IReviewRepository reviews)
{
    private const int PageSize = 20;

    public async Task<Result<IReadOnlyList<Review>>> ExecuteAsync(
        Guid subjectId,
        int page,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        int offset = (page - 1) * PageSize;
        var items = await reviews.ListBySubjectAsync(subjectId, offset, PageSize + 1, cancellationToken);
        return Result<IReadOnlyList<Review>>.Success(items);
    }
}

public sealed class GetSubjectRatingUseCase(IReviewRepository reviews)
{
    public async Task<Result<SubjectRating>> ExecuteAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var avg = await reviews.GetSubjectAverageAsync(subjectId, cancellationToken);
        return Result<SubjectRating>.Success(
            avg.HasValue
                ? new SubjectRating(avg.Value.Average, avg.Value.Count)
                : new SubjectRating(0, 0));
    }
}
