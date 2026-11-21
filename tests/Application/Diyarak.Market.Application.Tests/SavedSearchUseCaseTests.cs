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
    }
}
