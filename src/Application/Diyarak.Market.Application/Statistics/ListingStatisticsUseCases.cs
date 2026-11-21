using Diyarak.Market.Application.Favorites;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Statistics;

public enum ListingStatistic
{
    View = 1,
    ContactReveal = 2,
}

public sealed record ListingDailyStatistics(DateOnly Day, int Views, int ContactReveals);

public sealed record ListingStatisticsTotals(Guid ListingId, int Views, int ContactReveals);

/// <summary>Daily counters per Listing (ADR-0067).</summary>
public interface IListingStatisticsStore
{
    /// <summary>Atomically adds one to the counter of <paramref name="day"/>.</summary>
    public Task IncrementAsync(Guid listingId, DateOnly day, ListingStatistic statistic, CancellationToken cancellationToken = default);

    /// <summary>Days with activity in the inclusive range; days without rows are omitted.</summary>
    public Task<IReadOnlyList<ListingDailyStatistics>> ListDailyAsync(Guid listingId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ListingStatisticsTotals>> SumAsync(IReadOnlyCollection<Guid> listingIds, DateOnly from, CancellationToken cancellationToken = default);
}

public static class ListingStatisticsErrors
{
    public const int MinimumDays = 7;

    public const int MaximumDays = 90;

    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.listing_statistics.invalid_request",
            $"The Listing, actor and a period of {MinimumDays}-{MaximumDays} days are required.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing_statistics.not_found",
            "The Listing does not exist.");
}

/// <summary>Statistics are kept per day in Syria's calendar so "today" matches what publishers see.</summary>
public static class StatisticsCalendar
{
    private static readonly TimeZoneInfo Damascus = FindDamascus();

    public static DateOnly Today(DateTimeOffset now) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Damascus).DateTime);

    public static int LocalHour(DateTimeOffset now) => TimeZoneInfo.ConvertTime(now, Damascus).Hour;

    private static TimeZoneInfo FindDamascus()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Damascus");
        }
        catch (TimeZoneNotFoundException)
        {
            // Syria has used UTC+3 all year since 2022.
            return TimeZoneInfo.CreateCustomTimeZone("Syria", TimeSpan.FromHours(3), "Syria", "Syria");
        }
    }
}

/// <summary>Counts a public view of a published Listing. The publisher's own views are ignored.</summary>
public sealed class RecordListingViewUseCase(
    IMarketListingRepository listings,
    IListingStatisticsStore statistics,
    IClock clock)
{
    /// <returns><c>true</c> when the view was counted.</returns>
    public async Task<bool> ExecuteAsync(Guid listingId, Guid? viewerUserId, CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
            return false;

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);
        if (listing is null || !listing.IsPubliclyVisible || listing.PublisherUserId == viewerUserId)
            return false;

        await statistics.IncrementAsync(listingId, StatisticsCalendar.Today(clock.UtcNow), ListingStatistic.View, cancellationToken);
        return true;
    }
}

public sealed record ListingStatisticsReport(
    Guid ListingId,
    DateOnly From,
    DateOnly To,
    int Views,
    int ContactReveals,
    int Favorites,
    IReadOnlyList<ListingDailyStatistics> Days);

/// <summary>The owner's statistics for one Listing, with every day of the period present.</summary>
public sealed class GetListingStatisticsUseCase(
    IMarketListingRepository listings,
    IListingStatisticsStore statistics,
    IListingFavoriteStore favorites,
    IClock clock)
{
    public async Task<Result<ListingStatisticsReport>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty ||
            days is < ListingStatisticsErrors.MinimumDays or > ListingStatisticsErrors.MaximumDays)
        {
            return Result.Failure<ListingStatisticsReport>(ListingStatisticsErrors.InvalidRequest);
        }

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);

        // Missing and non-owned Listings are indistinguishable (ADR-0023).
        if (listing is null || listing.PublisherUserId != actorUserId)
            return Result.Failure<ListingStatisticsReport>(ListingStatisticsErrors.NotFound);

        DateOnly to = StatisticsCalendar.Today(clock.UtcNow);
        DateOnly from = to.AddDays(1 - days);

        Dictionary<DateOnly, ListingDailyStatistics> recorded =
            (await statistics.ListDailyAsync(listingId, from, to, cancellationToken)).ToDictionary(day => day.Day);

        ListingDailyStatistics[] series =
        [
            .. Enumerable.Range(0, days)
                .Select(offset => from.AddDays(offset))
                .Select(day => recorded.TryGetValue(day, out ListingDailyStatistics? value) ? value : new ListingDailyStatistics(day, 0, 0)),
        ];

        int favoriteCount = await favorites.CountForListingAsync(listingId, cancellationToken);

        return Result.Success(new ListingStatisticsReport(
            listingId,
            from,
            to,
            series.Sum(day => day.Views),
            series.Sum(day => day.ContactReveals),
            favoriteCount,
            series));
    }
}

/// <summary>Totals over the period for every Listing of the actor (the "my listings" overview).</summary>
public sealed class ListOwnedListingStatisticsUseCase(
    IMarketListingRepository listings,
    IListingStatisticsStore statistics,
    IClock clock)
{
    public async Task<Result<IReadOnlyList<ListingStatisticsTotals>>> ExecuteAsync(
        Guid actorUserId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty ||
            days is < ListingStatisticsErrors.MinimumDays or > ListingStatisticsErrors.MaximumDays)
        {
            return Result.Failure<IReadOnlyList<ListingStatisticsTotals>>(ListingStatisticsErrors.InvalidRequest);
        }

        IReadOnlyList<MarketListing> owned = await listings.FindByPublisherUserIdAsync(actorUserId, cancellationToken);
        if (owned.Count == 0)
            return Result.Success<IReadOnlyList<ListingStatisticsTotals>>([]);

        DateOnly from = StatisticsCalendar.Today(clock.UtcNow).AddDays(1 - days);
        IReadOnlyList<ListingStatisticsTotals> totals =
            await statistics.SumAsync([.. owned.Select(listing => listing.Id)], from, cancellationToken);

        return Result.Success(totals);
    }
}
