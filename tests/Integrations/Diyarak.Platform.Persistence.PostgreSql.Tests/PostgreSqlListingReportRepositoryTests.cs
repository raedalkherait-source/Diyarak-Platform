using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlListingReportRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reports_round_trip_and_resolutions_persist()
    {
        string database = Guid.NewGuid().ToString();
        Guid listingId = Guid.NewGuid();

        await using (PlatformDbContext context = Create(database))
        {
            var repository = new PostgreSqlListingReportRepository(context);
            await repository.AddAsync(new ListingReport(Guid.NewGuid(), listingId, ListingReportReason.FraudOrScam, "x", Now));
            await repository.AddAsync(new ListingReport(Guid.NewGuid(), listingId, ListingReportReason.Duplicate, null, Now.AddMinutes(5)));
            await repository.AddAsync(new ListingReport(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Other, null, Now.AddMinutes(1)));
        }

        await using (PlatformDbContext context = Create(database))
        {
            var repository = new PostgreSqlListingReportRepository(context);
            Assert.Equal(3, (await repository.ListOpenAsync(0, 10)).Count);
            Assert.Equal(2, await repository.CountRecentAsync(listingId, Now));
            Assert.Equal(1, await repository.CountRecentAsync(listingId, Now.AddMinutes(1)));

            IReadOnlyList<ListingReport> open = await repository.ListOpenByListingAsync(listingId);
            Assert.Equal([ListingReportReason.FraudOrScam, ListingReportReason.Duplicate], open.Select(report => report.Reason));

            Guid moderator = Guid.NewGuid();
            foreach (ListingReport report in open)
                report.Resolve(ListingReportResolution.Dismissed, moderator, Now.AddHours(1));
            await repository.SaveResolutionsAsync(open);
        }

        await using (PlatformDbContext context = Create(database))
        {
            var repository = new PostgreSqlListingReportRepository(context);
            Assert.Empty(await repository.ListOpenByListingAsync(listingId));
            Assert.Single(await repository.ListOpenAsync(0, 10));
            MarketListingReportRecord stored = await context.MarketListingReports.FirstAsync(report => report.ListingId == listingId);
            Assert.Equal((int)ListingReportResolution.Dismissed, stored.Resolution);
            Assert.Equal(Now.AddHours(1), stored.ResolvedAt);
        }
    }

    [Fact]
    public async Task Suspended_listings_round_trip_and_are_listed_by_status()
    {
        string database = Guid.NewGuid().ToString();
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();
        listing.Suspend("صور مضللة");

        await using (PlatformDbContext context = Create(database))
        {
            context.MarketListings.Add(MarketListingRecordMapper.FromDomain(listing));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = Create(database))
        {
            MarketListing restored = Assert.Single(
                await new PostgreSqlListingReportRepository(context).ListByStatusAsync(ListingStatus.Suspended, 0, 10));
            Assert.Equal("صور مضللة", restored.ModerationNote);
            Assert.Empty(await new PostgreSqlListingReportRepository(context).ListByStatusAsync(ListingStatus.Published, 0, 10));
        }
    }

    private static PlatformDbContext Create(string database) =>
        new(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(database).Options);
}
