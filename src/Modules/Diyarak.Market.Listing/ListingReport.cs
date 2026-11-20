using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

public enum ListingReportReason
{
    FraudOrScam = 1,
    IncorrectInformation = 2,
    AlreadySoldOrRented = 3,
    Duplicate = 4,
    InappropriateContent = 5,
    Other = 6,
}

public enum ListingReportResolution
{
    Dismissed = 1,
    ListingSuspended = 2,
}

/// <summary>A visitor's report that a published listing breaks the rules or is wrong.</summary>
public sealed class ListingReport : AggregateRoot<Guid>
{
    public const int MaximumCommentLength = 500;

    public ListingReport(
        Guid id,
        Guid listingId,
        ListingReportReason reason,
        string? comment,
        DateTimeOffset createdAt)
        : base(id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Report identifier cannot be empty.", nameof(id));

        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing identifier cannot be empty.", nameof(listingId));

        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unsupported report reason.");

        string? trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        if (trimmed is { Length: > MaximumCommentLength })
            throw new ArgumentException("Report comment is too long.", nameof(comment));

        ListingId = listingId;
        Reason = reason;
        Comment = trimmed;
        CreatedAt = createdAt;
    }

    public Guid ListingId { get; }

    public ListingReportReason Reason { get; }

    public string? Comment { get; }

    public DateTimeOffset CreatedAt { get; }

    public ListingReportResolution? Resolution { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public bool IsOpen => Resolution is null;

    public static ListingReport Restore(
        Guid id,
        Guid listingId,
        ListingReportReason reason,
        string? comment,
        DateTimeOffset createdAt,
        ListingReportResolution? resolution,
        DateTimeOffset? resolvedAt,
        Guid? resolvedByUserId)
    {
        var report = new ListingReport(id, listingId, reason, comment, createdAt);

        if (resolution is { } value)
        {
            if (resolvedAt is null || resolvedByUserId is null)
                throw new ArgumentException("A resolved report requires resolution time and moderator.");

            report.Resolve(value, resolvedByUserId.Value, resolvedAt.Value);
        }

        return report;
    }

    public void Resolve(ListingReportResolution resolution, Guid moderatorUserId, DateTimeOffset resolvedAt)
    {
        if (!Enum.IsDefined(resolution))
            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unsupported resolution.");

        if (moderatorUserId == Guid.Empty)
            throw new ArgumentException("Moderator identifier cannot be empty.", nameof(moderatorUserId));

        if (!IsOpen)
            throw new InvalidOperationException("The report is already resolved.");

        Resolution = resolution;
        ResolvedByUserId = moderatorUserId;
        ResolvedAt = resolvedAt;
    }
}
