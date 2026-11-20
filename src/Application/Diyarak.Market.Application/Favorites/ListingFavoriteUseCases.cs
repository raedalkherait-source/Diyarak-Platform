using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Favorites;

/// <summary>Stores which Listings a user has saved.</summary>
public interface IListingFavoriteStore
{
    /// <returns><c>false</c> when the Listing was already saved.</returns>
    public Task<bool> AddAsync(
        Guid userId,
        Guid listingId,
        DateTimeOffset savedAt,
        CancellationToken cancellationToken = default);

    public Task RemoveAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    public Task<int> CountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    public Task<bool> ContainsAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saved Listings that are currently published, most recently saved first. Saved Listings that
    /// are paused, closed or suspended are skipped but kept, so they return if republished.
    /// </summary>
    public Task<IReadOnlyList<PublishedListingProjection>> ListPublishedAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public static class ListingFavoriteErrors
{
    public const int MaximumFavorites = 200;

    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.favorite.invalid_request",
            "The Listing identifier, actor and pagination must be valid.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.favorite.listing_not_found",
            "The Listing does not exist or is not published.");

    public static Error LimitReached { get; } =
        Error.Conflict(
            "market.favorite.limit_reached",
            $"A user can save at most {MaximumFavorites} Listings.");
}

public sealed record FavoriteListingPage(
    IReadOnlyList<PublishedListingProjection> Items,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>Saves a published Listing for the actor. Saving twice is not an error.</summary>
public sealed class AddListingFavoriteUseCase(
    IMarketListingRepository listings,
    IListingFavoriteStore favorites,
    IMarketTransactionRunner transactionRunner,
    IClock clock)
{
    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty)
            return Result.Failure(ListingFavoriteErrors.InvalidRequest);

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);
        if (listing is null || !listing.IsPubliclyVisible)
            return Result.Failure(ListingFavoriteErrors.NotFound);

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                if (await favorites.ContainsAsync(actorUserId, listingId, token))
                    return Result.Success();

                if (await favorites.CountAsync(actorUserId, token) >= ListingFavoriteErrors.MaximumFavorites)
                    return Result.Failure(ListingFavoriteErrors.LimitReached);

                await favorites.AddAsync(actorUserId, listingId, clock.UtcNow, token);
                return Result.Success();
            },
            cancellationToken);
    }
}

/// <summary>Removes a saved Listing. Removing one that is not saved is not an error.</summary>
public sealed class RemoveListingFavoriteUseCase(IListingFavoriteStore favorites)
{
    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty)
            return Result.Failure(ListingFavoriteErrors.InvalidRequest);

        await favorites.RemoveAsync(actorUserId, listingId, cancellationToken);
        return Result.Success();
    }
}

public sealed class GetListingFavoriteStatusUseCase(IListingFavoriteStore favorites)
{
    public async Task<Result<bool>> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty)
            return Result.Failure<bool>(ListingFavoriteErrors.InvalidRequest);

        return Result.Success(await favorites.ContainsAsync(actorUserId, listingId, cancellationToken));
    }
}

public sealed class ListListingFavoritesUseCase(
    IListingFavoriteStore favorites,
    IListingPhotoRepository photos)
{
    public const int MaximumPageSize = 100;

    public async Task<Result<FavoriteListingPage>> ExecuteAsync(
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty ||
            page < 1 ||
            pageSize is < 1 or > MaximumPageSize ||
            (long)(page - 1) * pageSize >= ListingFavoriteErrors.MaximumFavorites)
        {
            return Result.Failure<FavoriteListingPage>(ListingFavoriteErrors.InvalidRequest);
        }

        IReadOnlyList<PublishedListingProjection> candidates = await favorites.ListPublishedAsync(
            actorUserId,
            (page - 1) * pageSize,
            pageSize + 1,
            cancellationToken);

        PublishedListingProjection[] items = [.. candidates.Take(pageSize)];

        if (items.Length > 0)
        {
            IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>> listingPhotos =
                await photos.ListByListingsAsync([.. items.Select(item => item.Listing.Id)], cancellationToken);

            items = [.. items.Select(item => listingPhotos.TryGetValue(item.Listing.Id, out IReadOnlyList<ListingPhoto>? found)
                ? item with { Photos = found }
                : item)];
        }

        return Result.Success(new FavoriteListingPage(items, page, pageSize, candidates.Count > pageSize));
    }
}
