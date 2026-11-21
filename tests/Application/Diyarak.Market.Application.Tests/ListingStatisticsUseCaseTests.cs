using Diyarak.Market.Application.Favorites;
using Diyarak.Market.Application.Statistics;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListingStatisticsUseCaseTests
{
    // 22:30 UTC is already the next day in Damascus (UTC+3).
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 22, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 21);

    [Fact]
    public async Task Views_of_published_listings_are_counted_on_the_Damascus_day_except_the_owners()
    {
        MarketListing listing = Create(publish: true);
        var store = new Store();
        var useCase = new RecordListingViewUseCase(new Listings(listing), store, new Clock());

        Assert.True(await useCase.ExecuteAsync(listing.Id, viewerUserId: null));
        Assert.True(await useCase.ExecuteAsync(listing.Id, Guid.NewGuid()));
        Assert.False(await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId));
        Assert.False(await useCase.ExecuteAsync(Guid.NewGuid(), null));

        Assert.Equal(2, store.Count(listing.Id, Today, ListingStatistic.View));
    }

    [Fact]
    public async Task Draft_views_are_not_counted()
    {
        MarketListing draft = Create(publish: false);
        var store = new Store();

        Assert.False(await new RecordListingViewUseCase(new Listings(draft), store, new Clock()).ExecuteAsync(draft.Id, null));
        Assert.Empty(store.Rows);
    }

    [Fact]
    public async Task Report_fills_every_day_and_totals_the_period()
    {
        MarketListing listing = Create(publish: true);
        var store = new Store();
        store.Add(listing.Id, Today, ListingStatistic.View, 5);
        store.Add(listing.Id, Today.AddDays(-3), ListingStatistic.View, 2);
        store.Add(listing.Id, Today.AddDays(-3), ListingStatistic.ContactReveal, 1);
        store.Add(listing.Id, Today.AddDays(-10), ListingStatistic.View, 100); // outside a 7-day window
        var favorites = new Favorites(3);

        Result<ListingStatisticsReport> report = await new GetListingStatisticsUseCase(new Listings(listing), store, favorites, new Clock())
            .ExecuteAsync(listing.Id, listing.PublisherUserId, 7);

        Assert.True(report.IsSuccess);
        Assert.Equal(7, report.Value.Days.Count);
        Assert.Equal(Today.AddDays(-6), report.Value.From);
        Assert.Equal(Today, report.Value.To);
        Assert.Equal(7, report.Value.Views);
        Assert.Equal(1, report.Value.ContactReveals);
        Assert.Equal(3, report.Value.Favorites);
        Assert.Equal(0, report.Value.Days[0].Views);
    }

    [Fact]
    public async Task Report_is_owner_only_and_validates_the_period()
    {
        MarketListing listing = Create(publish: true);
        var useCase = new GetListingStatisticsUseCase(new Listings(listing), new Store(), new Favorites(0), new Clock());

        Assert.Equal(ListingStatisticsErrors.NotFound, (await useCase.ExecuteAsync(listing.Id, Guid.NewGuid(), 30)).Error);
        Assert.Equal(ListingStatisticsErrors.InvalidRequest, (await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, 6)).Error);
        Assert.Equal(ListingStatisticsErrors.InvalidRequest, (await useCase.ExecuteAsync(listing.Id, listing.PublisherUserId, 91)).Error);
    }

    [Fact]
    public async Task Contact_reveals_are_counted_when_a_store_is_configured()
    {
        MarketListing listing = Create(publish: true);
        listing.SetContact(new ListingContactOptions(true, true));
        var store = new Store();
        var useCase = new GetListingContactUseCase(new Listings(listing), new Phones(), store, new Clock());

        Assert.True((await useCase.ExecuteAsync(listing.Id)).IsSuccess);
        Assert.Equal(1, store.Count(listing.Id, Today, ListingStatistic.ContactReveal));
    }

    private static MarketListing Create(bool publish)
    {
        var listing = new MarketListing(Guid.NewGuid(), Guid.NewGuid(), new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        if (publish)
            listing.Publish(Now);
        return listing;
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Phones : IPublisherPhoneLookup
    {
        public Task<PhoneNumber?> FindVerifiedPhoneAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PhoneNumber?>(PhoneNumber.Create("+963912345678"));
    }

    private sealed class Store : IListingStatisticsStore
    {
        public Dictionary<(Guid, DateOnly, ListingStatistic), int> Rows { get; } = [];

        public int Count(Guid id, DateOnly day, ListingStatistic statistic) => Rows.GetValueOrDefault((id, day, statistic));

        public void Add(Guid id, DateOnly day, ListingStatistic statistic, int amount) =>
            Rows[(id, day, statistic)] = Count(id, day, statistic) + amount;

        public Task IncrementAsync(Guid listingId, DateOnly day, ListingStatistic statistic, CancellationToken cancellationToken = default)
        {
            Add(listingId, day, statistic, 1);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ListingDailyStatistics>> ListDailyAsync(Guid listingId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingDailyStatistics>>(
            [
                .. Rows.Keys.Where(key => key.Item1 == listingId && key.Item2 >= from && key.Item2 <= to)
                    .Select(key => key.Item2).Distinct().Order()
                    .Select(day => new ListingDailyStatistics(day, Count(listingId, day, ListingStatistic.View), Count(listingId, day, ListingStatistic.ContactReveal))),
            ]);

        public Task<IReadOnlyList<ListingStatisticsTotals>> SumAsync(IReadOnlyCollection<Guid> listingIds, DateOnly from, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingStatisticsTotals>>([]);
    }

    private sealed class Favorites(int count) : IListingFavoriteStore
    {
        public Task<bool> AddAsync(Guid userId, Guid listingId, DateTimeOffset savedAt, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task RemoveAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<bool> ContainsAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> CountForListingAsync(Guid listingId, CancellationToken cancellationToken = default) => Task.FromResult(count);

        public Task<IReadOnlyList<PublishedListingProjection>> ListPublishedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublishedListingProjection>>([]);
    }

    private sealed class Listings(MarketListing listing) : IMarketListingRepository
    {
        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(listing.Id == listingId ? listing : null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([listing]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
