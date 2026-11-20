using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListingContactUseCaseTests
{
    private static readonly Guid Publisher = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly PhoneNumber Phone = PhoneNumber.Create("+963912345678");

    [Fact]
    public async Task Update_sets_contact_when_publisher_has_verified_phone()
    {
        MarketListing listing = CreateListing();
        var repository = new SingleListingRepository(listing);
        var useCase = new UpdateListingUseCase(repository, new StubMarketTransactionRunner(), new PhoneLookup(Phone));

        Result<long> result = await useCase.ExecuteAsync(listing.Id, Publisher, ContactPatch(new ListingContactOptions(true, true)));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ListingContactOptions(true, true), listing.Contact);
    }

    [Fact]
    public async Task Update_rejects_contact_without_verified_phone()
    {
        MarketListing listing = CreateListing();
        var useCase = new UpdateListingUseCase(
            new SingleListingRepository(listing),
            new StubMarketTransactionRunner(),
            new PhoneLookup(null));

        Result<long> result = await useCase.ExecuteAsync(listing.Id, Publisher, ContactPatch(new ListingContactOptions(false, true)));

        Assert.Equal(UpdateListingErrors.ContactPhoneNotVerified, result.Error);
        Assert.Null(listing.Contact);
    }

    [Fact]
    public async Task Update_with_null_contact_clears_without_phone_lookup()
    {
        MarketListing listing = CreateListing();
        listing.SetContact(new ListingContactOptions(true, false));
        var useCase = new UpdateListingUseCase(new SingleListingRepository(listing), new StubMarketTransactionRunner());

        Result<long> result = await useCase.ExecuteAsync(listing.Id, Publisher, ContactPatch(null));

        Assert.True(result.IsSuccess);
        Assert.Null(listing.Contact);
    }

    [Fact]
    public async Task Contact_is_revealed_only_for_published_opted_in_listing_with_verified_phone()
    {
        MarketListing listing = CreateListing();
        var repository = new SingleListingRepository(listing);
        var useCase = new GetListingContactUseCase(repository, new PhoneLookup(Phone));

        listing.SetContact(new ListingContactOptions(false, true));
        Assert.Equal(GetListingContactErrors.NotFound, (await useCase.ExecuteAsync(listing.Id)).Error);

        listing.Publish();
        Result<ListingContactDetails> revealed = await useCase.ExecuteAsync(listing.Id);

        Assert.True(revealed.IsSuccess);
        Assert.Equal(Phone, revealed.Value.PhoneNumber);
        Assert.False(revealed.Value.AllowCalls);
        Assert.True(revealed.Value.AllowWhatsApp);

        Assert.Equal(GetListingContactErrors.NotFound, (await useCase.ExecuteAsync(Guid.NewGuid())).Error);
        Assert.Equal(GetListingContactErrors.NotFound, (await useCase.ExecuteAsync(Guid.Empty)).Error);

        var withoutPhone = new GetListingContactUseCase(repository, new PhoneLookup(null));
        Assert.Equal(GetListingContactErrors.NotFound, (await withoutPhone.ExecuteAsync(listing.Id)).Error);
    }

    [Fact]
    public async Task Published_listing_without_contact_options_reveals_nothing()
    {
        MarketListing listing = CreateListing();
        listing.Publish();
        var useCase = new GetListingContactUseCase(new SingleListingRepository(listing), new PhoneLookup(Phone));

        Assert.Equal(GetListingContactErrors.NotFound, (await useCase.ExecuteAsync(listing.Id)).Error);
    }

    private static UpdateListingPatch ContactPatch(ListingContactOptions? contact) =>
        new(1, false, null, false, null, false, null, false, null, UpdateContact: true, Contact: contact);

    private static MarketListing CreateListing()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Publisher,
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Rent));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        return listing;
    }

    private sealed class PhoneLookup(PhoneNumber? phone) : IPublisherPhoneLookup
    {
        public Task<PhoneNumber?> FindVerifiedPhoneAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == Publisher ? phone : null);
    }

    private sealed class SingleListingRepository(MarketListing listing) : IMarketListingRepository
    {
        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(listing.Id == listingId ? listing : null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([listing]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
