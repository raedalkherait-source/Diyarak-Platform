using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlPublishedListingQueryTests
{
    [Fact]
    public async Task ListPageAsync_returns_only_published_listings()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing published = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true);
        MarketListing draft = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: false);

        context.MarketListings.AddRange(
            MarketListingRecordMapper.FromDomain(published),
            MarketListingRecordMapper.FromDomain(draft));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);

        IReadOnlyList<MarketListing> result =
            await query.ListPageAsync(skip: 0, take: 10);

        MarketListing item = Assert.Single(result);
        Assert.Equal(published.Id, item.Id);
        Assert.Equal(ListingStatus.Published, item.Status);
    }

    [Fact]
    public async Task ListPageAsync_orders_by_identifier_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing first = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true);
        MarketListing second = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true);
        MarketListing third = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            published: true);

        context.MarketListings.AddRange(
            MarketListingRecordMapper.FromDomain(third),
            MarketListingRecordMapper.FromDomain(first),
            MarketListingRecordMapper.FromDomain(second));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);

        IReadOnlyList<MarketListing> result =
            await query.ListPageAsync(skip: 1, take: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(second.Id, result[0].Id);
        Assert.Equal(third.Id, result[1].Id);
    }

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }

    private static MarketListing CreateListing(
        Guid listingId,
        bool published)
    {
        var listing = new MarketListing(
            listingId,
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

        if (published)
            listing.Publish();

        return listing;
    }
}
