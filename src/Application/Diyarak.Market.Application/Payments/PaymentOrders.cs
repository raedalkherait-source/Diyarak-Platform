using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Payments;

/// <summary>What can be bought (ADR-0071).</summary>
public enum PaymentProduct
{
    FeaturedListing7Days = 1,
    FeaturedListing30Days = 2,
}

/// <summary>Local payment channels; each is confirmed manually by an administrator.</summary>
public enum PaymentMethod
{
    BankTransfer = 1,
    SyriatelCash = 2,
    MtnCash = 3,
}

public enum PaymentOrderStatus
{
    Pending = 1,
    Paid = 2,
    Rejected = 3,
}

public sealed record PaymentPrice(decimal Amount, string Currency);

/// <summary>Prices and payment instructions, configured by the operator.</summary>
public interface IPaymentCatalog
{
    public PaymentPrice? FindPrice(PaymentProduct product);

    /// <summary>Instructions shown to the payer (account, wallet number…); <c>null</c> when the method is not offered.</summary>
    public string? FindInstructions(PaymentMethod method);
}

public static class PaymentProducts
{
    public static int FeaturedDays(PaymentProduct product) => product switch
    {
        PaymentProduct.FeaturedListing7Days => 7,
        PaymentProduct.FeaturedListing30Days => 30,
        _ => throw new ArgumentOutOfRangeException(nameof(product)),
    };
}

/// <summary>An order paid outside the platform and confirmed by an administrator.</summary>
public sealed class PaymentOrder
{
    public const int MinimumReferenceLength = 3;
    public const int MaximumReferenceLength = 60;
    public const int MaximumNoteLength = 300;

    private PaymentOrder(
        Guid id,
        Guid userId,
        PaymentProduct product,
        Guid listingId,
        PaymentPrice price,
        PaymentMethod method,
        string payerReference,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Product = product;
        ListingId = listingId;
        Price = price;
        Method = method;
        PayerReference = payerReference;
        CreatedAt = createdAt;
        Status = PaymentOrderStatus.Pending;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public PaymentProduct Product { get; }
    public Guid ListingId { get; }
    public PaymentPrice Price { get; }
    public PaymentMethod Method { get; }

    /// <summary>Transfer or wallet transaction number entered by the payer.</summary>
    public string PayerReference { get; }

    public DateTimeOffset CreatedAt { get; }
    public PaymentOrderStatus Status { get; private set; }
    public string? DecisionNote { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public Guid? DecidedBy { get; private set; }

    public static PaymentOrder Create(Guid id, Guid userId, PaymentProduct product, Guid listingId, PaymentPrice price, PaymentMethod method, string payerReference, DateTimeOffset createdAt)
    {
        string reference = (payerReference ?? string.Empty).Trim();
        if (reference.Length is < MinimumReferenceLength or > MaximumReferenceLength || reference.Any(char.IsControl))
            throw new ArgumentException("The payment reference is invalid.", nameof(payerReference));

        if (!Enum.IsDefined(product) || !Enum.IsDefined(method))
            throw new ArgumentException("Unknown product or payment method.");

        return new PaymentOrder(id, userId, product, listingId, price, method, reference, createdAt);
    }

    public static PaymentOrder Restore(
        Guid id, Guid userId, PaymentProduct product, Guid listingId, PaymentPrice price, PaymentMethod method, string payerReference,
        DateTimeOffset createdAt, PaymentOrderStatus status, string? decisionNote, DateTimeOffset? decidedAt, Guid? decidedBy) =>
        new(id, userId, product, listingId, price, method, payerReference, createdAt)
        {
            Status = status,
            DecisionNote = decisionNote,
            DecidedAt = decidedAt,
            DecidedBy = decidedBy,
        };

    public void Confirm(Guid moderatorId, DateTimeOffset at)
    {
        EnsurePending();
        Status = PaymentOrderStatus.Paid;
        DecidedAt = at;
        DecidedBy = moderatorId;
    }

    public void Reject(Guid moderatorId, string note, DateTimeOffset at)
    {
        EnsurePending();
        string trimmed = (note ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaximumNoteLength)
            throw new ArgumentException("A rejection needs a short note.", nameof(note));

        Status = PaymentOrderStatus.Rejected;
        DecisionNote = trimmed;
        DecidedAt = at;
        DecidedBy = moderatorId;
    }

    private void EnsurePending()
    {
        if (Status != PaymentOrderStatus.Pending)
            throw new InvalidOperationException("Only a pending order can be decided.");
    }
}

public interface IPaymentOrderStore
{
    public Task AddAsync(PaymentOrder order, CancellationToken cancellationToken = default);

    public Task<PaymentOrder?> FindAsync(Guid orderId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PaymentOrder>> ListByUserAsync(Guid userId, int take, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PaymentOrder>> ListByStatusAsync(PaymentOrderStatus status, int take, CancellationToken cancellationToken = default);

    public Task<bool> HasPendingForListingAsync(Guid listingId, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> when the order was decided meanwhile.</returns>
    public Task<bool> TrySaveDecisionAsync(PaymentOrder order, CancellationToken cancellationToken = default);
}

/// <summary>Featured periods per Listing.</summary>
public interface IListingPromotionStore
{
    /// <summary>The end of the Listing's latest promotion that is still running at <paramref name="now"/>.</summary>
    public Task<DateTimeOffset?> FindActiveUntilAsync(Guid listingId, DateTimeOffset now, CancellationToken cancellationToken = default);

    public Task AddAsync(Guid listingId, Guid orderId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken = default);
}

public static class PaymentErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.payment.invalid_request", "The product, payment method and reference must be valid.");

    public static Error NotOffered { get; } =
        Error.Validation("market.payment.not_offered", "This product or payment method is not offered.");

    public static Error ListingNotFound { get; } =
        Error.NotFound("market.payment.listing_not_found", "The Listing does not exist.");

    public static Error ListingNotPublished { get; } =
        Error.Conflict("market.payment.listing_not_published", "Only a published Listing can be featured.");

    public static Error PendingExists { get; } =
        Error.Conflict("market.payment.pending_exists", "This Listing already has a payment awaiting confirmation.");

    public static Error NotFound { get; } =
        Error.NotFound("market.payment.not_found", "The order does not exist.");

    public static Error AlreadyDecided { get; } =
        Error.Conflict("market.payment.already_decided", "The order was already confirmed or rejected.");
}

/// <summary>The owner orders a featured period for one of their published Listings.</summary>
public sealed class CreatePaymentOrderUseCase(
    IMarketListingRepository listings,
    IPaymentOrderStore orders,
    IPaymentCatalog catalog,
    IMarketTransactionRunner transactionRunner,
    IClock clock)
{
    public async Task<Result<PaymentOrder>> ExecuteAsync(
        Guid actorUserId,
        Guid listingId,
        PaymentProduct product,
        PaymentMethod method,
        string payerReference,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || listingId == Guid.Empty || !Enum.IsDefined(product) || !Enum.IsDefined(method))
            return Result.Failure<PaymentOrder>(PaymentErrors.InvalidRequest);

        if (catalog.FindPrice(product) is not { } price || catalog.FindInstructions(method) is null)
            return Result.Failure<PaymentOrder>(PaymentErrors.NotOffered);

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                MarketListing? listing = await listings.FindByIdAsync(listingId, token);
                if (listing is null || listing.PublisherUserId != actorUserId)
                    return Result.Failure<PaymentOrder>(PaymentErrors.ListingNotFound);

                if (listing.Status != ListingStatus.Published)
                    return Result.Failure<PaymentOrder>(PaymentErrors.ListingNotPublished);

                if (await orders.HasPendingForListingAsync(listingId, token))
                    return Result.Failure<PaymentOrder>(PaymentErrors.PendingExists);

                PaymentOrder order;
                try
                {
                    order = PaymentOrder.Create(Guid.NewGuid(), actorUserId, product, listingId, price, method, payerReference, clock.UtcNow);
                }
                catch (ArgumentException)
                {
                    return Result.Failure<PaymentOrder>(PaymentErrors.InvalidRequest);
                }

                await orders.AddAsync(order, token);
                return Result.Success(order);
            },
            cancellationToken);
    }
}

public enum PaymentDecision
{
    Confirm = 1,
    Reject = 2,
}

/// <summary>
/// An administrator confirms a payment they verified in the bank or wallet account, which starts
/// (or extends) the featured period; or rejects it with a note.
/// </summary>
public sealed class DecidePaymentOrderUseCase(
    IPaymentOrderStore orders,
    IListingPromotionStore promotions,
    IMarketTransactionRunner transactionRunner,
    IClock clock)
{
    public async Task<Result<PaymentOrder>> ExecuteAsync(
        Guid orderId,
        Guid moderatorId,
        PaymentDecision decision,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty || moderatorId == Guid.Empty || !Enum.IsDefined(decision))
            return Result.Failure<PaymentOrder>(PaymentErrors.InvalidRequest);

        return await transactionRunner.ExecuteAsync(
            async token =>
            {
                PaymentOrder? order = await orders.FindAsync(orderId, token);
                if (order is null)
                    return Result.Failure<PaymentOrder>(PaymentErrors.NotFound);

                DateTimeOffset now = clock.UtcNow;
                try
                {
                    if (decision == PaymentDecision.Confirm)
                        order.Confirm(moderatorId, now);
                    else
                        order.Reject(moderatorId, note ?? string.Empty, now);
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure<PaymentOrder>(PaymentErrors.AlreadyDecided);
                }
                catch (ArgumentException)
                {
                    return Result.Failure<PaymentOrder>(PaymentErrors.InvalidRequest);
                }

                if (!await orders.TrySaveDecisionAsync(order, token))
                    return Result.Failure<PaymentOrder>(PaymentErrors.AlreadyDecided);

                if (decision == PaymentDecision.Confirm)
                {
                    // A new period starts when the current one ends, so paying early never loses days.
                    DateTimeOffset start = await promotions.FindActiveUntilAsync(order.ListingId, now, token) is { } until && until > now ? until : now;
                    await promotions.AddAsync(order.ListingId, order.Id, start, start.AddDays(PaymentProducts.FeaturedDays(order.Product)), token);
                }

                return Result.Success(order);
            },
            cancellationToken);
    }
}
