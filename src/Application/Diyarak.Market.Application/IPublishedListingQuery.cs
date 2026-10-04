namespace Diyarak.Market.Application;

public interface IPublishedListingQuery
{
    public Task<IReadOnlyList<PublishedListingProjection>> ListPageAsync(
        int skip,
        int take,
        PublishedListingSearchCriteria? criteria = null,
        CancellationToken cancellationToken = default);
}
