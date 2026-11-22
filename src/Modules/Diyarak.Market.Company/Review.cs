using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Company;

public enum ReviewSubjectType
{
    Contractor = 1,
    Agency = 2,
}

public sealed class Review : AggregateRoot<Guid>
{
    public const int MinimumRating = 1;
    public const int MaximumRating = 5;
    public const int MaximumCommentLength = 1000;

    private Review(Guid id, Guid authorUserId, Guid subjectId, ReviewSubjectType subjectType)
        : base(id)
    {
        if (authorUserId == Guid.Empty)
            throw new ArgumentException("A review needs an author.", nameof(authorUserId));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("A review needs a subject.", nameof(subjectId));
        if (!Enum.IsDefined(subjectType))
            throw new ArgumentException("Invalid subject type.", nameof(subjectType));

        AuthorUserId = authorUserId;
        SubjectId = subjectId;
        SubjectType = subjectType;
    }

    public Guid AuthorUserId { get; }

    public Guid SubjectId { get; }

    public ReviewSubjectType SubjectType { get; }

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Review Create(
        Guid id,
        Guid authorUserId,
        Guid subjectId,
        ReviewSubjectType subjectType,
        int rating,
        string? comment,
        DateTimeOffset createdAt)
    {
        var review = new Review(id, authorUserId, subjectId, subjectType)
        {
            CreatedAt = createdAt,
        };
        review.SetRatingAndComment(rating, comment);
        return review;
    }

    public static Review Restore(
        Guid id,
        Guid authorUserId,
        Guid subjectId,
        ReviewSubjectType subjectType,
        int rating,
        string? comment,
        DateTimeOffset createdAt)
    {
        var review = new Review(id, authorUserId, subjectId, subjectType)
        {
            Rating = rating,
            Comment = comment,
            CreatedAt = createdAt,
        };
        return review;
    }

    private void SetRatingAndComment(int rating, string? comment)
    {
        if (rating is < MinimumRating or > MaximumRating)
            throw new ArgumentException($"Rating must be between {MinimumRating} and {MaximumRating}.", nameof(rating));

        string? trimmedComment = comment?.Trim();
        if (trimmedComment is { Length: > MaximumCommentLength })
            throw new ArgumentException($"Comment exceeds {MaximumCommentLength} characters.", nameof(comment));
        if (trimmedComment is { Length: > 0 } && trimmedComment.Any(c => char.IsControl(c) && c is not '\n' and not '\r'))
            throw new ArgumentException("Comment contains control characters.", nameof(comment));

        Rating = rating;
        Comment = string.IsNullOrEmpty(trimmedComment) ? null : trimmedComment;
    }
}
