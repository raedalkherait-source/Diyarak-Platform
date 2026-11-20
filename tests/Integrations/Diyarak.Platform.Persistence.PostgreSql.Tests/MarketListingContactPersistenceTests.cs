using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class MarketListingContactPersistenceTests
{
    [Fact]
    public void Mapper_round_trips_contact_options_on_published_listing()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.SetContact(new ListingContactOptions(false, true));
        listing.Publish();

        MarketListingRecord record = MarketListingRecordMapper.FromDomain(listing);
        MarketListing restored = MarketListingRecordMapper.ToDomain(record);

        Assert.False(record.ContactAllowCalls);
        Assert.True(record.ContactAllowWhatsApp);
        Assert.Equal(listing.Contact, restored.Contact);
        Assert.Equal(ListingStatus.Published, restored.Status);
    }

    [Fact]
    public void Mapper_rejects_half_persisted_contact_options()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        MarketListingRecord record = MarketListingRecordMapper.FromDomain(listing);
        record.ContactAllowCalls = true;

        Assert.Throws<InvalidOperationException>(() => MarketListingRecordMapper.ToDomain(record));
    }

    [Fact]
    public async Task Phone_lookup_returns_verified_phone_for_user()
    {
        await using var context = new PlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Guid userId = Guid.NewGuid();
        context.PhoneAccounts.Add(new PhoneAccountRecord { PhoneNumber = "+963912345678", UserId = userId, CreatedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var lookup = new PostgreSqlPublisherPhoneLookup(context);

        Assert.Equal(PhoneNumber.Create("+963912345678"), await lookup.FindVerifiedPhoneAsync(userId));
        Assert.Null(await lookup.FindVerifiedPhoneAsync(Guid.NewGuid()));
    }
}
