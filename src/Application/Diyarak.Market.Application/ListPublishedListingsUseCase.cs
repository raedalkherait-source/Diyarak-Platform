using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class ListPublishedListingsUseCase
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IPublishedListingQuery _publishedListingQuery;

    public ListPublishedListingsUseCase(
        IPublishedListingQuery publishedListingQuery)
    {
        ArgumentNullException.ThrowIfNull(publishedListingQuery);
        _publishedListingQuery = publishedListingQuery;
    }

    public async Task<Result<PublishedListingPage>> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0 ||
            pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidPagination);
        }

        long skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return Result.Failure<PublishedListingPage>(
                ListPublishedListingsErrors.InvalidPagination);
        }

        IReadOnlyList<MarketListing> candidates =
            await _publishedListingQuery.ListPageAsync(
                (int)skip,
                pageSize + 1,
                cancellationToken);

        if (candidates.Any(
                listing =>
                    listing.Status != ListingStatus.Published))
        {
            throw new InvalidOperationException(
                "The published Listing query returned a non-published Listing.");
        }

        bool hasMore = candidates.Count > pageSize;
        MarketListing[] items = candidates
            .Take(pageSize)
            .ToArray();

        return Result.Success(
            new PublishedListingPage(
                items,
                page,
                pageSize,
                hasMore));
    }
}
