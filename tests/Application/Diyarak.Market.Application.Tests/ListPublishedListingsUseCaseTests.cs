using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListPublishedListingsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_rejects_non_positive_page_without_querying()
    {
        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 0,
                pageSize: 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidPagination,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_page_size_above_maximum_without_querying()
    {
        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize:
                    ListPublishedListingsUseCase.MaximumPageSize + 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidPagination,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_requests_one_extra_row_and_reports_has_more()
    {
        MarketListing first = CreatePublishedListing();
        MarketListing second = CreatePublishedListing();
        MarketListing third = CreatePublishedListing();
        var query = new StubPublishedListingQuery(
            [first, second, third]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 2,
                pageSize: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, query.LastSkip);
        Assert.Equal(3, query.LastTake);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Same(first, result.Value.Items[0]);
        Assert.Same(second, result.Value.Items[1]);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.True(result.Value.HasMore);
    }

    [Fact]
    public async Task ExecuteAsync_fails_closed_when_query_returns_draft_listing()
    {
        var draft = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));
        var query = new StubPublishedListingQuery([draft]);
        var useCase = new ListPublishedListingsUseCase(query);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                page: 1,
                pageSize: 20));
    }

    [Fact]
    public async Task ExecuteAsync_reports_last_page_when_query_returns_no_extra_row()
    {
        MarketListing listing = CreatePublishedListing();
        var query = new StubPublishedListingQuery([listing]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 2);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.HasMore);
    }

    private static MarketListing CreatePublishedListing()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));
        listing.SetHeadline(
            new ListingHeadline("Published property"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();

        return listing;
    }

    private sealed class StubPublishedListingQuery(
        IReadOnlyList<MarketListing> result)
        : IPublishedListingQuery
    {
        public int CallCount { get; private set; }

        public int LastSkip { get; private set; }

        public int LastTake { get; private set; }

        public Task<IReadOnlyList<MarketListing>> ListPageAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSkip = skip;
            LastTake = take;
            return Task.FromResult(result);
        }
    }
}
