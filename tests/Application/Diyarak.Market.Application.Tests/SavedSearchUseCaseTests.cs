using Diyarak.Market.Application.SavedSearches;
using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class SavedSearchUseCaseTests
{
    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public async Task Creates_a_trimmed_search_and_rejects_duplicates()
    {
        var store = new Store();
        CreateSavedSearchUseCase useCase = new(store, new StubMarketTransactionRunner(), new Clock());

        Result<SavedSearch> created = await useCase.ExecuteAsync(User, "  شقق في المزة ", "governorate=Damascus");
        Result<SavedSearch> again = await useCase.ExecuteAsync(User, "other name", "governorate=Damascus");

        Assert.True(created.IsSuccess);
        Assert.Equal("شقق في المزة", created.Value.Name);
        Assert.Equal(Clock.Now, created.Value.LastSeenAt);
        Assert.Equal(SavedSearchErrors.Duplicate, again.Error);
        Assert.Single(store.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\nname")]
    public async Task Rejects_invalid_names(string name)
    {
        Result<SavedSearch> result = await new CreateSavedSearchUseCase(new Store(), new StubMarketTransactionRunner(), new Clock())
            .ExecuteAsync(User, name, "governorate=Damascus");

        Assert.Equal(SavedSearchErrors.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task Enforces_the_per_user_limit()
    {
        var store = new Store();
        for (int index = 0; index < SavedSearchErrors.MaximumSearches; index++)
            store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "s", $"q={index}", Clock.Now, Clock.Now));

        Result<SavedSearch> result = await new CreateSavedSearchUseCase(store, new StubMarketTransactionRunner(), new Clock())
            .ExecuteAsync(User, "one more", "governorate=Aleppo");

        Assert.Equal(SavedSearchErrors.LimitReached, result.Error);
    }

    [Fact]
    public async Task Lists_with_new_counts_since_last_seen_and_caps_them()
    {
        var store = new Store();
        var seen = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "few", "a", seen, seen));
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "many", "b", seen, seen));
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "broken", "c", seen, seen));
        var listings = new CountingQuery(new Dictionary<string, int> { ["a"] = 3, ["b"] = 250 });

        Result<IReadOnlyList<SavedSearchSummary>> result = await new ListSavedSearchesUseCase(store, listings)
            .ExecuteAsync(User, query => query == "c" ? null : new PublishedListingSearchCriteria(TransactionIntents: null) { PublishedAfter = null, Property = new PublishedPropertySearchCriteria(Location: new PublishedPropertyLocationSearchCriteria(query, null)) });

        Assert.True(result.IsSuccess);
        Assert.Equal([3, 99, 0], result.Value.Select(summary => summary.NewListingCount));
        Assert.Equal([false, true, false], result.Value.Select(summary => summary.NewListingCountCapped));
        Assert.All(listings.Seen, criteria => Assert.Equal(seen, criteria.PublishedAfter));
    }

    [Fact]
    public async Task Seen_and_delete_are_scoped_to_the_owner()
    {
        var store = new Store();
        var search = new SavedSearch(Guid.NewGuid(), User, "s", "q", Clock.Now.AddDays(-1), Clock.Now.AddDays(-1));
        store.Items.Add(search);

        Assert.Equal(SavedSearchErrors.NotFound, (await new MarkSavedSearchSeenUseCase(store, new Clock()).ExecuteAsync(Guid.NewGuid(), search.Id)).Error);
        Assert.True((await new MarkSavedSearchSeenUseCase(store, new Clock()).ExecuteAsync(User, search.Id)).IsSuccess);
        Assert.Equal(Clock.Now, store.Items.Single().LastSeenAt);

        Assert.Equal(SavedSearchErrors.NotFound, (await new DeleteSavedSearchUseCase(store).ExecuteAsync(Guid.NewGuid(), search.Id)).Error);
        Assert.True((await new DeleteSavedSearchUseCase(store).ExecuteAsync(User, search.Id)).IsSuccess);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Alert_run_sends_one_summary_per_user_and_respects_the_daily_interval()
    {
        var store = new Store();
        var seen = Clock.Now.AddDays(-2);
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "a", "a", seen, seen, AlertsEnabled: true));
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "b", "b", seen, seen, AlertsEnabled: true));
        store.Items.Add(new SavedSearch(Guid.NewGuid(), User, "off", "c", seen, seen, AlertsEnabled: false));
        Guid quietUser = Guid.NewGuid();
        store.Items.Add(new SavedSearch(Guid.NewGuid(), quietUser, "none", "d", seen, seen, AlertsEnabled: true));
        var listings = new CountingQuery(new Dictionary<string, int> { ["a"] = 2, ["b"] = 3, ["c"] = 50 });
        var notifier = new Notifier();
        var useCase = new SendSavedSearchAlertsUseCase(store, listings, notifier, new Clock());
        SavedSearchCriteriaParser parse = query => new PublishedListingSearchCriteria(Property: new PublishedPropertySearchCriteria(Location: new PublishedPropertyLocationSearchCriteria(query, null)));

        SavedSearchAlertRun first = await useCase.ExecuteAsync(parse, 100);

        Assert.Equal(2, first.UsersChecked);
        Assert.Equal(1, first.AlertsSent);
        Assert.Equal((User, 5), Assert.Single(notifier.Sent));
        Assert.All(store.Items.Where(item => item.UserId == User && item.AlertsEnabled), item => Assert.Equal(Clock.Now, item.LastAlertedAt));
        Assert.Null(store.Items.Single(item => item.UserId == quietUser).LastAlertedAt);

        SavedSearchAlertRun second = await useCase.ExecuteAsync(parse, 100);
        Assert.Equal(0, second.AlertsSent);
        Assert.Single(notifier.Sent);
    }

    [Fact]
    public void Alerts_count_from_the_later_of_last_visit_and_last_alert()
    {
        var visited = Clock.Now.AddDays(-3);
        var alerted = Clock.Now.AddDays(-1);
        var search = new SavedSearch(Guid.NewGuid(), User, "a", "a", visited, visited, AlertsEnabled: true, LastAlertedAt: alerted);
        Assert.Equal(alerted, search.AlertSince);
        Assert.Equal(Clock.Now, (search with { LastSeenAt = Clock.Now }).AlertSince);
    }

    [Fact]
    public async Task Alerts_toggle_is_owner_scoped()
    {
        var store = new Store();
        var search = new SavedSearch(Guid.NewGuid(), User, "s", "q", Clock.Now, Clock.Now);
        store.Items.Add(search);
        var useCase = new SetSavedSearchAlertsUseCase(store);

        Assert.Equal(SavedSearchErrors.NotFound, (await useCase.ExecuteAsync(Guid.NewGuid(), search.Id, true)).Error);
        Assert.True((await useCase.ExecuteAsync(User, search.Id, true)).IsSuccess);
        Assert.True(store.Items.Single().AlertsEnabled);
    }

    private sealed class Clock : IClock
    {
        public static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => Now;
    }

    private sealed class CountingQuery(Dictionary<string, int> counts) : IPublishedListingQuery
    {
        public List<PublishedListingSearchCriteria> Seen { get; } = [];

        public Task<IReadOnlyList<PublishedListingProjection>> ListPageAsync(int skip, int take, PublishedListingSearchCriteria? criteria = null, CancellationToken cancellationToken = default)
        {
            Seen.Add(criteria!);
            string key = criteria!.Property!.Location!.City!;
            int count = Math.Min(counts.GetValueOrDefault(key), take);
            return Task.FromResult<IReadOnlyList<PublishedListingProjection>>([.. Enumerable.Repeat<PublishedListingProjection>(null!, count)]);
        }
    }

    private sealed class Store : ISavedSearchStore
    {
        public List<SavedSearch> Items { get; } = [];

        public Task AddAsync(SavedSearch search, CancellationToken cancellationToken = default)
        {
            Items.Add(search);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SavedSearch>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedSearch>>([.. Items.Where(item => item.UserId == userId)]);

        public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(item => item.UserId == userId));

        public Task<bool> ExistsWithQueryAsync(Guid userId, string query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(item => item.UserId == userId && item.Query == query));

        public Task<bool> MarkSeenAsync(Guid userId, Guid searchId, DateTimeOffset seenAt, CancellationToken cancellationToken = default)
        {
            int index = Items.FindIndex(item => item.Id == searchId && item.UserId == userId);
            if (index < 0)
                return Task.FromResult(false);

            Items[index] = Items[index] with { LastSeenAt = seenAt };
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid userId, Guid searchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.RemoveAll(item => item.Id == searchId && item.UserId == userId) == 1);

        public Task<bool> SetAlertsAsync(Guid userId, Guid searchId, bool enabled, CancellationToken cancellationToken = default)
        {
            int index = Items.FindIndex(item => item.Id == searchId && item.UserId == userId);
            if (index < 0)
                return Task.FromResult(false);

            Items[index] = Items[index] with { AlertsEnabled = enabled };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Guid>> ListUsersDueForAlertsAsync(DateTimeOffset alertedBefore, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
            [
                .. Items.Where(item => item.AlertsEnabled)
                    .GroupBy(item => item.UserId)
                    .Where(group => group.Max(item => item.LastAlertedAt) is not { } last || last < alertedBefore)
                    .Select(group => group.Key)
                    .Take(take),
            ]);

        public Task MarkAlertedAsync(Guid userId, DateTimeOffset alertedAt, CancellationToken cancellationToken = default)
        {
            for (int index = 0; index < Items.Count; index++)
            {
                if (Items[index].UserId == userId && Items[index].AlertsEnabled)
                    Items[index] = Items[index] with { LastAlertedAt = alertedAt };
            }

            return Task.CompletedTask;
        }
    }

    private sealed class Notifier(bool reachable = true) : ISavedSearchAlertNotifier
    {
        public List<(Guid UserId, int Count)> Sent { get; } = [];

        public Task<bool> NotifyAsync(Guid userId, int newListingCount, CancellationToken cancellationToken = default)
        {
            if (reachable)
                Sent.Add((userId, newListingCount));
            return Task.FromResult(reachable);
        }
    }
}
