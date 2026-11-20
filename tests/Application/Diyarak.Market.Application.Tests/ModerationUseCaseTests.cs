using Diyarak.Market.Application.Moderation;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ModerationUseCaseTests
{
    private static readonly Guid Moderator = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly Listings _listings = new();
    private readonly Reports _reports = new();
    private readonly Clock _clock = new();

    [Fact]
    public async Task Visitor_can_report_only_public_listings()
    {
        MarketListing published = Add(publish: true);
        MarketListing draft = Add(publish: false);
        var useCase = new ReportListingUseCase(_listings, _reports, _clock);

        Result<Guid> reported = await useCase.ExecuteAsync(published.Id, ListingReportReason.FraudOrScam, "طلب تحويل");

        Assert.True(reported.IsSuccess);
        Assert.Single(_reports.Items);
        Assert.Equal(ModerationErrors.ListingNotFound, (await useCase.ExecuteAsync(draft.Id, ListingReportReason.Other, null)).Error);
        Assert.Equal(ModerationErrors.InvalidRequest, (await useCase.ExecuteAsync(published.Id, (ListingReportReason)77, null)).Error);
    }

    [Fact]
    public async Task Reports_per_listing_are_capped_per_hour()
    {
        MarketListing listing = Add(publish: true);
        var useCase = new ReportListingUseCase(_listings, _reports, _clock);

        for (int i = 0; i < ReportListingUseCase.MaximumReportsPerListingPerHour; i++)
            Assert.True((await useCase.ExecuteAsync(listing.Id, ListingReportReason.Duplicate, null)).IsSuccess);

        Assert.Equal(ModerationErrors.TooManyReports, (await useCase.ExecuteAsync(listing.Id, ListingReportReason.Duplicate, null)).Error);
    }

    [Fact]
    public async Task Queue_groups_open_reports_by_listing()
    {
        MarketListing first = Add(publish: true);
        MarketListing second = Add(publish: true);
        var report = new ReportListingUseCase(_listings, _reports, _clock);
        await report.ExecuteAsync(first.Id, ListingReportReason.FraudOrScam, null);
        await report.ExecuteAsync(first.Id, ListingReportReason.IncorrectInformation, null);
        await report.ExecuteAsync(second.Id, ListingReportReason.Duplicate, null);

        IReadOnlyList<ModerationQueueItem> queue = await new ListModerationQueueUseCase(_listings, _reports).ExecuteAsync();

        Assert.Equal(2, queue.Count);
        Assert.Equal(2, queue.Single(item => item.Listing.Id == first.Id).OpenReports.Count);
    }

    [Fact]
    public async Task Suspend_hides_listing_and_resolves_open_reports()
    {
        MarketListing listing = Add(publish: true);
        await new ReportListingUseCase(_listings, _reports, _clock).ExecuteAsync(listing.Id, ListingReportReason.FraudOrScam, null);
        ModerateListingUseCase moderate = CreateModerate();

        Result result = await moderate.ExecuteAsync(listing.Id, Moderator, ModerationAction.Suspend, "احتيال مؤكد");

        Assert.True(result.IsSuccess);
        Assert.Equal(ListingStatus.Suspended, listing.Status);
        ListingReport report = Assert.Single(_reports.Items);
        Assert.Equal(ListingReportResolution.ListingSuspended, report.Resolution);
        Assert.Equal(Moderator, report.ResolvedByUserId);

        Assert.True((await moderate.ExecuteAsync(listing.Id, Moderator, ModerationAction.Reinstate)).IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    [Fact]
    public async Task Dismiss_resolves_reports_without_touching_listing()
    {
        MarketListing listing = Add(publish: true);
        await new ReportListingUseCase(_listings, _reports, _clock).ExecuteAsync(listing.Id, ListingReportReason.Other, null);

        Assert.True((await CreateModerate().ExecuteAsync(listing.Id, Moderator, ModerationAction.DismissReports)).IsSuccess);

        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(ListingReportResolution.Dismissed, Assert.Single(_reports.Items).Resolution);
        Assert.Empty(await new ListModerationQueueUseCase(_listings, _reports).ExecuteAsync());
    }

    [Fact]
    public async Task Invalid_moderation_requests_are_rejected()
    {
        MarketListing listing = Add(publish: true);
        ModerateListingUseCase moderate = CreateModerate();

        Assert.Equal(ModerationErrors.InvalidRequest, (await moderate.ExecuteAsync(listing.Id, Moderator, ModerationAction.Suspend, " ")).Error);
        Assert.Equal(ModerationErrors.InvalidTransition, (await moderate.ExecuteAsync(listing.Id, Moderator, ModerationAction.Reinstate)).Error);
        Assert.Equal(ModerationErrors.ListingNotFound, (await moderate.ExecuteAsync(Guid.NewGuid(), Moderator, ModerationAction.DismissReports)).Error);
        Assert.Equal(ModerationErrors.InvalidRequest, (await moderate.ExecuteAsync(listing.Id, Guid.Empty, ModerationAction.DismissReports)).Error);
    }

    private ModerateListingUseCase CreateModerate() => new(_listings, _reports, new StubMarketTransactionRunner(), _clock);

    private MarketListing Add(bool publish)
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        if (publish)
            listing.Publish();
        _listings.Items[listing.Id] = listing;
        return listing;
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class Listings : IMarketListingRepository
    {
        public Dictionary<Guid, MarketListing> Items { get; } = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class Reports : IListingReportRepository
    {
        public List<ListingReport> Items { get; } = [];

        public Task AddAsync(ListingReport report, CancellationToken cancellationToken = default)
        {
            Items.Add(report);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ListingReport>> ListOpenAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingReport>>([.. Items.Where(report => report.IsOpen).Skip(skip).Take(take)]);

        public Task<IReadOnlyList<ListingReport>> ListOpenByListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingReport>>([.. Items.Where(report => report.IsOpen && report.ListingId == listingId)]);

        public Task<int> CountRecentAsync(Guid listingId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(report => report.ListingId == listingId && report.CreatedAt >= since));

        public Task SaveResolutionsAsync(IReadOnlyCollection<ListingReport> reports, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
