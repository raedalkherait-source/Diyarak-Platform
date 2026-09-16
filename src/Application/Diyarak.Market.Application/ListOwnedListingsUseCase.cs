using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

public sealed class ListOwnedListingsUseCase
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IMarketListingRepository _listingRepository;

    public ListOwnedListingsUseCase(
        IMarketListingRepository listingRepository)
    {
        ArgumentNullException.ThrowIfNull(listingRepository);
        _listingRepository = listingRepository;
    }

    public async Task<Result<OwnedListingPage>> ExecuteAsync(
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<OwnedListingPage>(
                ListOwnedListingsErrors.InvalidActorIdentifier);
        }

        if (page <= 0 ||
            pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            return Result.Failure<OwnedListingPage>(
                ListOwnedListingsErrors.InvalidPagination);
        }

        long skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return Result.Failure<OwnedListingPage>(
                ListOwnedListingsErrors.InvalidPagination);
        }

        IReadOnlyList<MarketListing> candidates =
            await _listingRepository.FindPageByPublisherUserIdAsync(
                actorUserId,
                (int)skip,
                pageSize + 1,
                cancellationToken);

        if (candidates.Any(
                listing =>
                    listing.PublisherUserId != actorUserId))
        {
            throw new InvalidOperationException(
                "The owned Listing query returned a Listing for another publisher.");
        }

        bool hasMore = candidates.Count > pageSize;
        MarketListing[] items = candidates
            .Take(pageSize)
            .ToArray();

        return Result.Success(
            new OwnedListingPage(
                items,
                page,
                pageSize,
                hasMore));
    }
}
