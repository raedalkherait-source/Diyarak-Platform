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
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.Context, restored.Context);
        Assert.Equal(original.Headline, restored.Headline);
        Assert.Equal(original.Price, restored.Price);
    }

    [Fact]
    public async Task FindByPublisherUserIdAsync_returns_empty_when_owner_has_no_listings()
    {
        await using PlatformDbContext context = CreateContext();

        var repository =
            new PostgreSqlMarketListingRepository(context);

        IReadOnlyList<MarketListing> listings =
            await repository.FindByPublisherUserIdAsync(
                Guid.NewGuid());

        Assert.Empty(listings);
    }

    [Fact]
    public async Task FindByPublisherUserIdAsync_returns_only_owned_listings()
    {
        await using PlatformDbContext context = CreateContext();

        Guid ownerUserId = Guid.NewGuid();
        MarketListing first = CreateListing(ownerUserId);
        MarketListing second = CreateListing(ownerUserId);
        MarketListing other = CreateListing(Guid.NewGuid());

        context.MarketListings.AddRange(
            MarketListingRecordMapper.FromDomain(first),
            MarketListingRecordMapper.FromDomain(second),
            MarketListingRecordMapper.FromDomain(other));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository =
            new PostgreSqlMarketListingRepository(context);

        IReadOnlyList<MarketListing> listings =
            await repository.FindByPublisherUserIdAsync(ownerUserId);

        Assert.Equal(2, listings.Count);
        Assert.Contains(
            listings,
            listing => listing.Id == first.Id);
        Assert.Contains(
            listings,
            listing => listing.Id == second.Id);
        Assert.DoesNotContain(
            listings,
            listing => listing.Id == other.Id);
        Assert.All(
            listings,
            listing =>
                Assert.Equal(
                    ownerUserId,
                    listing.PublisherUserId));
    }

    [Fact]
    public async Task FindPageByPublisherUserIdAsync_filters_orders_and_bounds_results()
    {
        await using PlatformDbContext context = CreateContext();

        Guid ownerUserId = Guid.NewGuid();
        MarketListing third = CreateListing(
            ownerUserId,
            Guid.Parse("00000000-0000-0000-0000-000000000003"));
        MarketListing first = CreateListing(
            ownerUserId,
            Guid.Parse("00000000-0000-0000-0000-000000000001"));
        MarketListing second = CreateListing(
            ownerUserId,
            Guid.Parse("00000000-0000-0000-0000-000000000002"));
        MarketListing other = CreateListing(Guid.NewGuid());

        context.MarketListings.AddRange(
            MarketListingRecordMapper.FromDomain(third),
            MarketListingRecordMapper.FromDomain(first),
            MarketListingRecordMapper.FromDomain(second),
            MarketListingRecordMapper.FromDomain(other));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new PostgreSqlMarketListingRepository(context);

        IReadOnlyList<MarketListing> page =
            await repository.FindPageByPublisherUserIdAsync(
                ownerUserId,
                skip: 1,
                take: 2);

        Assert.Equal(new[] { second.Id, third.Id }, page.Select(item => item.Id));
        Assert.All(page, item => Assert.Equal(ownerUserId, item.PublisherUserId));
        Assert.Empty(context.ChangeTracker.Entries());
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
        Assert.Equal(1, persisted.Version);
    }

    [Fact]
    public async Task TrySaveAsync_persists_published_listing_state()
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

        long expectedVersion = loaded.Version;

        bool saved = await repository.TrySaveAsync(
            loaded,
            expectedVersion);

        Assert.True(saved);

        context.ChangeTracker.Clear();

        MarketListingRecord persisted =
            await context.MarketListings.SingleAsync(
                record => record.Id == original.Id);

        Assert.Equal(
            (int)ListingStatus.Published,
            persisted.Status);
        Assert.Equal(expectedVersion + 1, persisted.Version);
    }

    [Fact]
    public async Task TrySaveAsync_rejects_stale_competing_publication()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

        MarketListing original = CreateReadyDraftListing();

        await using (var seedContext = new PlatformDbContext(options))
        {
            seedContext.MarketListings.Add(
                MarketListingRecordMapper.FromDomain(original));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new PlatformDbContext(options);
        await using var secondContext = new PlatformDbContext(options);

        var firstRepository =
            new PostgreSqlMarketListingRepository(firstContext);
        var secondRepository =
            new PostgreSqlMarketListingRepository(secondContext);

        MarketListing? first =
            await firstRepository.FindByIdAsync(original.Id);
        MarketListing? second =
            await secondRepository.FindByIdAsync(original.Id);

        Assert.NotNull(first);
        Assert.NotNull(second);

        first!.Publish();
        second!.Publish();

        long expectedVersion = first.Version;
        Assert.Equal(expectedVersion, second.Version);

        bool firstSaved =
            await firstRepository.TrySaveAsync(
                first,
                expectedVersion);
        bool secondSaved =
            await secondRepository.TrySaveAsync(
                second,
                expectedVersion);

        Assert.True(firstSaved);
        Assert.False(secondSaved);

        await using var verifyContext =
            new PlatformDbContext(options);

        MarketListingRecord persisted =
            await verifyContext.MarketListings.SingleAsync(
                record => record.Id == original.Id);

        Assert.Equal(
            (int)ListingStatus.Published,
            persisted.Status);
        Assert.Equal(expectedVersion + 1, persisted.Version);
    }

    [Fact]
    public async Task TrySaveAsync_rejects_stale_competing_draft_edit()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

        MarketListing original = CreateReadyDraftListing();

        await using (var seedContext = new PlatformDbContext(options))
        {
            seedContext.MarketListings.Add(
                MarketListingRecordMapper.FromDomain(original));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new PlatformDbContext(options);
        await using var secondContext = new PlatformDbContext(options);

        var firstRepository =
            new PostgreSqlMarketListingRepository(firstContext);
        var secondRepository =
            new PostgreSqlMarketListingRepository(secondContext);

        MarketListing first = Assert.IsType<MarketListing>(
            await firstRepository.FindByIdAsync(original.Id));
        MarketListing second = Assert.IsType<MarketListing>(
            await secondRepository.FindByIdAsync(original.Id));

        long expectedVersion = first.Version;
        Assert.Equal(expectedVersion, second.Version);

        first.SetHeadline(new ListingHeadline("First edit"));
        second.SetHeadline(new ListingHeadline("Second edit"));

        bool firstSaved =
            await firstRepository.TrySaveAsync(first, expectedVersion);
        bool secondSaved =
            await secondRepository.TrySaveAsync(second, expectedVersion);

        Assert.True(firstSaved);
        Assert.False(secondSaved);

        await using var verifyContext = new PlatformDbContext(options);
        MarketListingRecord persisted =
            await verifyContext.MarketListings.SingleAsync(
                record => record.Id == original.Id);

        Assert.Equal("First edit", persisted.Headline);
        Assert.Equal(expectedVersion + 1, persisted.Version);
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
        Guid publisherUserId) =>
        CreateListing(publisherUserId, Guid.NewGuid());

    private static MarketListing CreateListing(
        Guid publisherUserId,
        Guid listingId) =>
        new(
            listingId,
            publisherUserId,
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

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
