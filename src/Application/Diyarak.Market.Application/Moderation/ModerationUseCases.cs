using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Moderation;

public static class ModerationErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.moderation.invalid_request", "The moderation request is invalid.");

    public static Error ListingNotFound { get; } =
        Error.NotFound("market.moderation.listing_not_found", "The Listing does not exist.");

    public static Error InvalidTransition { get; } =
        Error.Conflict("market.moderation.invalid_transition", "The Listing's current status does not allow this action.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict("market.moderation.concurrent_modification", "The Listing changed while the action was being applied.");

    public static Error TooManyReports { get; } =
        Error.Conflict("market.moderation.too_many_reports", "This Listing has already been reported many times recently.");
}

/// <summary>Lets any visitor report a publicly visible listing.</summary>
public sealed class ReportListingUseCase(
    IMarketListingRepository listings,
    IListingReportRepository reports,
    IClock clock)
{
    /// <summary>Caps stored reports per listing per hour so a single listing cannot flood the queue.</summary>
    public const int MaximumReportsPerListingPerHour = 20;

    public async Task<Result<Guid>> ExecuteAsync(
        Guid listingId,
        ListingReportReason reason,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || !Enum.IsDefined(reason) || comment is { Length: > ListingReport.MaximumCommentLength })
            return Result.Failure<Guid>(ModerationErrors.InvalidRequest);

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);

        if (listing is not { IsPubliclyVisible: true })
            return Result.Failure<Guid>(ModerationErrors.ListingNotFound);

        DateTimeOffset now = clock.UtcNow;

        if (await reports.CountRecentAsync(listingId, now.AddHours(-1), cancellationToken) >= MaximumReportsPerListingPerHour)
            return Result.Failure<Guid>(ModerationErrors.TooManyReports);

        var report = new ListingReport(Guid.NewGuid(), listingId, reason, comment, now);
        await reports.AddAsync(report, cancellationToken);
        return Result.Success(report.Id);
    }
}

public sealed record ModerationQueueItem(
    MarketListing Listing,
    IReadOnlyList<ListingReport> OpenReports);

/// <summary>Open reports grouped by listing, oldest report first.</summary>
public sealed class ListModerationQueueUseCase(
    IMarketListingRepository listings,
    IListingReportRepository reports)
{
    public const int MaximumReportsScanned = 500;

    public async Task<IReadOnlyList<ModerationQueueItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ListingReport> open = await reports.ListOpenAsync(0, MaximumReportsScanned, cancellationToken);
        var items = new List<ModerationQueueItem>();

        foreach (IGrouping<Guid, ListingReport> group in open.GroupBy(report => report.ListingId))
        {
            MarketListing? listing = await listings.FindByIdAsync(group.Key, cancellationToken);
            if (listing is not null)
                items.Add(new ModerationQueueItem(listing, [.. group]));
        }

        return items;
    }
}

public sealed class ListSuspendedListingsUseCase(IListingModerationQuery query)
{
    public Task<IReadOnlyList<MarketListing>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        query.ListByStatusAsync(ListingStatus.Suspended, 0, 200, cancellationToken);
}

public sealed class GetListingForModerationUseCase(IMarketListingRepository listings, IListingReportRepository reports)
{
    public async Task<Result<ModerationQueueItem>> ExecuteAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        MarketListing? listing = listingId == Guid.Empty ? null : await listings.FindByIdAsync(listingId, cancellationToken);

        return listing is null
            ? Result.Failure<ModerationQueueItem>(ModerationErrors.ListingNotFound)
            : Result.Success(new ModerationQueueItem(listing, await reports.ListOpenByListingAsync(listingId, cancellationToken)));
    }
}

public enum ModerationAction
{
    Suspend = 1,
    Reinstate = 2,
    DismissReports = 3,
}

/// <summary>Applies a moderator decision to a listing and resolves its open reports.</summary>
public sealed class ModerateListingUseCase(
    IMarketListingRepository listings,
    IListingReportRepository reports,
    IMarketTransactionRunner transactionRunner,
    IClock clock)
{
    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid moderatorUserId,
        ModerationAction action,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty ||
            moderatorUserId == Guid.Empty ||
            !Enum.IsDefined(action) ||
            (action == ModerationAction.Suspend &&
             (string.IsNullOrWhiteSpace(note) || note.Trim().Length > MarketListing.MaximumModerationNoteLength)))
        {
            return Result.Failure(ModerationErrors.InvalidRequest);
        }

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                MarketListing? listing = await listings.FindByIdAsync(listingId, token);
                if (listing is null)
                    return Result.Failure(ModerationErrors.ListingNotFound);

                IReadOnlyList<ListingReport> open = await reports.ListOpenByListingAsync(listingId, token);
                DateTimeOffset now = clock.UtcNow;

                if (action == ModerationAction.DismissReports)
                {
                    foreach (ListingReport report in open)
                        report.Resolve(ListingReportResolution.Dismissed, moderatorUserId, now);

                    await reports.SaveResolutionsAsync(open, token);
                    return Result.Success();
                }

                long expectedVersion = listing.Version;

                try
                {
                    if (action == ModerationAction.Suspend)
                        listing.Suspend(note!);
                    else
                        listing.Reinstate();
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure(ModerationErrors.InvalidTransition);
                }

                if (!await listings.TrySaveAsync(listing, expectedVersion, token))
                    return Result.Failure(ModerationErrors.ConcurrentModification);

                if (action == ModerationAction.Suspend)
                {
                    foreach (ListingReport report in open)
                        report.Resolve(ListingReportResolution.ListingSuspended, moderatorUserId, now);

                    await reports.SaveResolutionsAsync(open, token);
                }

                return Result.Success();
            },
            cancellationToken);
    }
}
