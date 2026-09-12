using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlMarketListingRepositoryTests
{
    [Fact]
    public async Task FindByIdAsync_returns_null_when_listing_is_missing()
    {
        await using PlatformDbContext context = CreateContext();

        var repository =
            new PostgreSqlMarketListingRepository(context);

        MarketListing? listing =
            await repository.FindByIdAsync(Guid.NewGuid());

        Assert.Null(listing);
    }

    [Fact]
    public async Task FindByIdAsync_restores_existing_draft_listing()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing original = CreateReadyDraftListing();

        context.MarketListings.Add(
            MarketListingRecordMapper.FromDomain(original));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository =
            new PostgreSqlMarketListingRepository(context);

        MarketListing? restored =
            await repository.FindByIdAsync(original.Id);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored!.Id);
        Assert.Equal(
            original.PublisherUserId,
            restored.PublisherUserId);
        Assert.Equal(
            original.SubjectReference,
            restored.SubjectReference);
        Assert.Equal(ListingStatus.Draft, restored.Status);
        Assert.Equal(original.Context, restored.Context);
        Assert.Equal(original.Headline, restored.Headline);
        Assert.Equal(original.Price, restored.Price);
    }

    [Fact]
    public async Task AddAsync_inserts_new_draft_listing()
    {
        await using PlatformDbContext context = CreateContext();

        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

        var repository =
            new PostgreSqlMarketListingRepository(context);

        await repository.AddAsync(listing);

        context.ChangeTracker.Clear();

        MarketListingRecord persisted =
            await context.MarketListings.SingleAsync(
                record => record.Id == listing.Id);

        Assert.Equal(listing.Id, persisted.Id);
        Assert.Equal(
            listing.PublisherUserId,
            persisted.PublisherUserId);
        Assert.Equal(
            listing.SubjectReference.SubjectId,
            persisted.SubjectId);
        Assert.Equal(
            MarketListingSubjectTypes.Property,
            persisted.SubjectType);
        Assert.Equal(
            (int)ListingStatus.Draft,
            persisted.Status);
    }

    [Fact]
    public async Task SaveAsync_persists_published_listing_state()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing original = CreateReadyDraftListing();

        context.MarketListings.Add(
            MarketListingRecordMapper.FromDomain(original));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository =
            new PostgreSqlMarketListingRepository(context);

        MarketListing? loaded =
            await repository.FindByIdAsync(original.Id);

        Assert.NotNull(loaded);

        loaded!.Publish();

        await repository.SaveAsync(loaded);

        context.ChangeTracker.Clear();

        MarketListingRecord persisted =
            await context.MarketListings.SingleAsync(
                record => record.Id == original.Id);

        Assert.Equal(
            (int)ListingStatus.Published,
            persisted.Status);
    }

    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }

    private static MarketListing CreateReadyDraftListing()
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
            new ListingHeadline("Property for sale"));

        listing.SetPrice(ListingPrice.OnRequest());

        return listing;
    }
}
