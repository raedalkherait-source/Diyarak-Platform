using Diyarak.Market.Application.Payments;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class PaymentUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Moderator = Guid.NewGuid();

    [Fact]
    public async Task Owner_orders_a_featured_period_for_a_published_listing()
    {
        MarketListing listing = Create(publish: true);
        var orders = new Orders();
        CreatePaymentOrderUseCase useCase = CreateUseCase(listing, orders);

        Result<PaymentOrder> created = await useCase.ExecuteAsync(listing.PublisherUserId, listing.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.SyriatelCash, " TX-778899 ");

        Assert.True(created.IsSuccess);
        Assert.Equal("TX-778899", created.Value.PayerReference);
        Assert.Equal(new PaymentPrice(50000, "SYP"), created.Value.Price);
        Assert.Equal(PaymentOrderStatus.Pending, created.Value.Status);

        Result<PaymentOrder> second = await useCase.ExecuteAsync(listing.PublisherUserId, listing.Id, PaymentProduct.FeaturedListing30Days, PaymentMethod.SyriatelCash, "TX-1");
        Assert.Equal(PaymentErrors.PendingExists, second.Error);
    }

    [Fact]
    public async Task Orders_are_rejected_for_drafts_strangers_unoffered_methods_and_bad_references()
    {
        MarketListing draft = Create(publish: false);
        MarketListing published = Create(publish: true);

        Assert.Equal(PaymentErrors.ListingNotPublished, (await CreateUseCase(draft, new Orders()).ExecuteAsync(draft.PublisherUserId, draft.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.BankTransfer, "REF-1")).Error);
        Assert.Equal(PaymentErrors.ListingNotFound, (await CreateUseCase(published, new Orders()).ExecuteAsync(Guid.NewGuid(), published.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.BankTransfer, "REF-1")).Error);
        Assert.Equal(PaymentErrors.NotOffered, (await CreateUseCase(published, new Orders()).ExecuteAsync(published.PublisherUserId, published.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.MtnCash, "REF-1")).Error);
        Assert.Equal(PaymentErrors.InvalidRequest, (await CreateUseCase(published, new Orders()).ExecuteAsync(published.PublisherUserId, published.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.BankTransfer, "x")).Error);
    }

    [Fact]
    public async Task Confirming_starts_or_extends_the_featured_period_and_cannot_be_repeated()
    {
        MarketListing listing = Create(publish: true);
        var orders = new Orders();
        var promotions = new Promotions();
        var decide = new DecidePaymentOrderUseCase(orders, promotions, new StubMarketTransactionRunner(), new Clock());

        PaymentOrder first = (await CreateUseCase(listing, orders).ExecuteAsync(listing.PublisherUserId, listing.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.BankTransfer, "BANK-1")).Value;
        Assert.True((await decide.ExecuteAsync(first.Id, Moderator, PaymentDecision.Confirm, null)).IsSuccess);
        Assert.Equal((Now, Now.AddDays(7)), promotions.Periods.Single());
        Assert.Equal(PaymentErrors.AlreadyDecided, (await decide.ExecuteAsync(first.Id, Moderator, PaymentDecision.Confirm, null)).Error);

        PaymentOrder second = (await CreateUseCase(listing, orders).ExecuteAsync(listing.PublisherUserId, listing.Id, PaymentProduct.FeaturedListing30Days, PaymentMethod.BankTransfer, "BANK-2")).Value;
        Assert.True((await decide.ExecuteAsync(second.Id, Moderator, PaymentDecision.Confirm, null)).IsSuccess);
        Assert.Equal((Now.AddDays(7), Now.AddDays(37)), promotions.Periods[1]);
    }

    [Fact]
    public async Task Rejection_needs_a_note_and_creates_no_promotion()
    {
        MarketListing listing = Create(publish: true);
        var orders = new Orders();
        var promotions = new Promotions();
        var decide = new DecidePaymentOrderUseCase(orders, promotions, new StubMarketTransactionRunner(), new Clock());
        PaymentOrder order = (await CreateUseCase(listing, orders).ExecuteAsync(listing.PublisherUserId, listing.Id, PaymentProduct.FeaturedListing7Days, PaymentMethod.BankTransfer, "BANK-1")).Value;

        Assert.Equal(PaymentErrors.InvalidRequest, (await decide.ExecuteAsync(order.Id, Moderator, PaymentDecision.Reject, " ")).Error);
        Result<PaymentOrder> rejected = await decide.ExecuteAsync(order.Id, Moderator, PaymentDecision.Reject, "لم يصل المبلغ");

        Assert.Equal(PaymentOrderStatus.Rejected, rejected.Value.Status);
        Assert.Empty(promotions.Periods);
    }

    private static CreatePaymentOrderUseCase CreateUseCase(MarketListing listing, Orders orders) =>
        new(new Listings(listing), orders, new Catalog(), new StubMarketTransactionRunner(), new Clock());

    private static MarketListing Create(bool publish)
    {
        var listing = new MarketListing(Guid.NewGuid(), Guid.NewGuid(), new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        if (publish)
            listing.Publish(Now);
        return listing;
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Catalog : IPaymentCatalog
    {
        public PaymentPrice? FindPrice(PaymentProduct product) => product == PaymentProduct.FeaturedListing7Days ? new PaymentPrice(50000, "SYP") : new PaymentPrice(150000, "SYP");

        public string? FindInstructions(PaymentMethod method) => method == PaymentMethod.MtnCash ? null : "حوّل إلى الحساب 123";
    }

    private sealed class Orders : IPaymentOrderStore
    {
        private readonly List<PaymentOrder> _items = [];

        public Task AddAsync(PaymentOrder order, CancellationToken cancellationToken = default)
        {
            _items.Add(order);
            return Task.CompletedTask;
        }

        public Task<PaymentOrder?> FindAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(order => order.Id == orderId) is { } found
                ? PaymentOrder.Restore(found.Id, found.UserId, found.Product, found.ListingId, found.Price, found.Method, found.PayerReference, found.CreatedAt, found.Status, found.DecisionNote, found.DecidedAt, found.DecidedBy)
                : null);

        public Task<IReadOnlyList<PaymentOrder>> ListByUserAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentOrder>>([.. _items.Where(order => order.UserId == userId)]);

        public Task<IReadOnlyList<PaymentOrder>> ListByStatusAsync(PaymentOrderStatus status, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentOrder>>([.. _items.Where(order => order.Status == status)]);

        public Task<bool> HasPendingForListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(order => order.ListingId == listingId && order.Status == PaymentOrderStatus.Pending));

        public Task<bool> TrySaveDecisionAsync(PaymentOrder order, CancellationToken cancellationToken = default)
        {
            int index = _items.FindIndex(item => item.Id == order.Id && item.Status == PaymentOrderStatus.Pending);
            if (index < 0)
                return Task.FromResult(false);

            _items[index] = order;
            return Task.FromResult(true);
        }
    }

    private sealed class Promotions : IListingPromotionStore
    {
        public List<(DateTimeOffset Start, DateTimeOffset End)> Periods { get; } = [];

        public Task<DateTimeOffset?> FindActiveUntilAsync(Guid listingId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(Periods.Where(period => period.End > now).Select(period => (DateTimeOffset?)period.End).Max());

        public Task AddAsync(Guid listingId, Guid orderId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken = default)
        {
            Periods.Add((startsAt, endsAt));
            return Task.CompletedTask;
        }
    }

    private sealed class Listings(MarketListing listing) : IMarketListingRepository
    {
        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(listing.Id == listingId ? listing : null);

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([listing]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
