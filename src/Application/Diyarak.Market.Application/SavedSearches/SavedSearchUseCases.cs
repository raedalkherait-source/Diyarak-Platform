using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.SavedSearches;

public sealed record SavedSearch(
    Guid Id,
    Guid UserId,
    string Name,
    string Query,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt);

/// <summary>A saved search with the number of Listings published since the user last opened it.</summary>
public sealed record SavedSearchSummary(
    SavedSearch Search,
    int NewListingCount,
    bool NewListingCountCapped);

public interface ISavedSearchStore
{
    public Task AddAsync(SavedSearch search, CancellationToken cancellationToken = default);

    /// <summary>The user's saved searches, newest first.</summary>
    public Task<IReadOnlyList<SavedSearch>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default);

    public Task<bool> ExistsWithQueryAsync(Guid userId, string query, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> when the search does not exist or belongs to someone else.</returns>
    public Task<bool> MarkSeenAsync(Guid userId, Guid searchId, DateTimeOffset seenAt, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> when the search does not exist or belongs to someone else.</returns>
    public Task<bool> DeleteAsync(Guid userId, Guid searchId, CancellationToken cancellationToken = default);
}

/// <summary>Turns a stored query into search criteria; owned by the API, which defines the query format.</summary>
public delegate PublishedListingSearchCriteria? SavedSearchCriteriaParser(string query);

public static class SavedSearchErrors
{
    public const int MaximumSearches = 20;

    public const int MaximumNameLength = 80;

    public const int MaximumQueryLength = 1000;

    /// <summary>"New" counts stop here; the portal shows "99+".</summary>
    public const int NewListingCountCap = 99;

    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.saved_search.invalid_request",
            "The name, search query and actor must be valid.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.saved_search.not_found",
            "The saved search does not exist.");

    public static Error LimitReached { get; } =
        Error.Conflict(
            "market.saved_search.limit_reached",
            $"A user can keep at most {MaximumSearches} saved searches.");

    public static Error Duplicate { get; } =
        Error.Conflict(
            "market.saved_search.duplicate",
            "This search is already saved.");
}

public sealed class CreateSavedSearchUseCase(
    ISavedSearchStore store,
    IMarketTransactionRunner transactionRunner,
    IClock clock)
{
    /// <summary>Saves a search. The caller has already validated and canonicalised the query.</summary>
    public async Task<Result<SavedSearch>> ExecuteAsync(
        Guid actorUserId,
        string name,
        string query,
        CancellationToken cancellationToken = default)
    {
        string trimmedName = (name ?? string.Empty).Trim();

        if (actorUserId == Guid.Empty ||
            trimmedName.Length is 0 or > SavedSearchErrors.MaximumNameLength ||
            trimmedName.Any(char.IsControl) ||
            query is null ||
            query.Length > SavedSearchErrors.MaximumQueryLength)
        {
            return Result.Failure<SavedSearch>(SavedSearchErrors.InvalidRequest);
        }

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                if (await store.ExistsWithQueryAsync(actorUserId, query, token))
                    return Result.Failure<SavedSearch>(SavedSearchErrors.Duplicate);

                if (await store.CountAsync(actorUserId, token) >= SavedSearchErrors.MaximumSearches)
                    return Result.Failure<SavedSearch>(SavedSearchErrors.LimitReached);

                DateTimeOffset now = clock.UtcNow;
                var search = new SavedSearch(Guid.NewGuid(), actorUserId, trimmedName, query, now, now);
                await store.AddAsync(search, token);
                return Result.Success(search);
            },
            cancellationToken);
    }
}

public sealed class ListSavedSearchesUseCase(
    ISavedSearchStore store,
    IPublishedListingQuery listings)
{
    public async Task<Result<IReadOnlyList<SavedSearchSummary>>> ExecuteAsync(
        Guid actorUserId,
        SavedSearchCriteriaParser parse,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parse);

        if (actorUserId == Guid.Empty)
            return Result.Failure<IReadOnlyList<SavedSearchSummary>>(SavedSearchErrors.InvalidRequest);

        IReadOnlyList<SavedSearch> searches = await store.ListAsync(actorUserId, cancellationToken);
        var summaries = new List<SavedSearchSummary>(searches.Count);

        foreach (SavedSearch search in searches)
        {
            // A query that no longer parses (e.g. after a format change) still lists, with no count.
            PublishedListingSearchCriteria? criteria = parse(search.Query);
            int count = 0;

            if (criteria is not null)
            {
                IReadOnlyList<PublishedListingProjection> fresh = await listings.ListPageAsync(
                    0,
                    SavedSearchErrors.NewListingCountCap + 1,
                    criteria with { PublishedAfter = search.LastSeenAt },
                    cancellationToken);
                count = fresh.Count;
            }

            summaries.Add(new SavedSearchSummary(
                search,
                Math.Min(count, SavedSearchErrors.NewListingCountCap),
                count > SavedSearchErrors.NewListingCountCap));
        }

        return Result.Success<IReadOnlyList<SavedSearchSummary>>(summaries);
    }
}

/// <summary>Records that the user opened the search, resetting its "new" count.</summary>
public sealed class MarkSavedSearchSeenUseCase(ISavedSearchStore store, IClock clock)
{
    public async Task<Result> ExecuteAsync(Guid actorUserId, Guid searchId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || searchId == Guid.Empty)
            return Result.Failure(SavedSearchErrors.InvalidRequest);

        return await store.MarkSeenAsync(actorUserId, searchId, clock.UtcNow, cancellationToken)
            ? Result.Success()
            : Result.Failure(SavedSearchErrors.NotFound);
    }
}

public sealed class DeleteSavedSearchUseCase(ISavedSearchStore store)
{
    public async Task<Result> ExecuteAsync(Guid actorUserId, Guid searchId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || searchId == Guid.Empty)
            return Result.Failure(SavedSearchErrors.InvalidRequest);

        return await store.DeleteAsync(actorUserId, searchId, cancellationToken)
            ? Result.Success()
            : Result.Failure(SavedSearchErrors.NotFound);
    }
}
