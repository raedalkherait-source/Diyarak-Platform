using Diyarak.Market.Application.Payments;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlPaymentOrderStore(PlatformDbContext context) : IPaymentOrderStore
{
    public async Task AddAsync(PaymentOrder order, CancellationToken cancellationToken = default)
    {
        MarketPaymentOrderRecord record = FromDomain(order);
        context.MarketPaymentOrders.Add(record);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            context.Entry(record).State = EntityState.Detached;
        }
    }

    public async Task<PaymentOrder?> FindAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        MarketPaymentOrderRecord? record = await context.MarketPaymentOrders.AsNoTracking()
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<PaymentOrder>> ListByUserAsync(Guid userId, int take, CancellationToken cancellationToken = default)
    {
        List<MarketPaymentOrderRecord> records = await context.MarketPaymentOrders.AsNoTracking()
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    public async Task<IReadOnlyList<PaymentOrder>> ListByStatusAsync(PaymentOrderStatus status, int take, CancellationToken cancellationToken = default)
    {
        List<MarketPaymentOrderRecord> records = await context.MarketPaymentOrders.AsNoTracking()
            .Where(order => order.Status == (int)status)
            .OrderBy(order => order.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    public Task<bool> HasPendingForListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        context.MarketPaymentOrders.AnyAsync(order => order.ListingId == listingId && order.Status == (int)PaymentOrderStatus.Pending, cancellationToken);

    public async Task<bool> TrySaveDecisionAsync(PaymentOrder order, CancellationToken cancellationToken = default)
    {
        // Only a still-pending row is updated, so two administrators cannot both decide one order.
        int updated = await context.MarketPaymentOrders
            .Where(record => record.Id == order.Id && record.Status == (int)PaymentOrderStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.Status, (int)order.Status)
                    .SetProperty(record => record.DecisionNote, order.DecisionNote)
                    .SetProperty(record => record.DecidedAt, order.DecidedAt)
                    .SetProperty(record => record.DecidedBy, order.DecidedBy),
                cancellationToken);
        return updated == 1;
    }

    private static MarketPaymentOrderRecord FromDomain(PaymentOrder order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        Product = (int)order.Product,
        ListingId = order.ListingId,
        Amount = order.Price.Amount,
        Currency = order.Price.Currency,
        Method = (int)order.Method,
        PayerReference = order.PayerReference,
        CreatedAt = order.CreatedAt,
        Status = (int)order.Status,
        DecisionNote = order.DecisionNote,
        DecidedAt = order.DecidedAt,
        DecidedBy = order.DecidedBy,
    };

    private static PaymentOrder ToDomain(MarketPaymentOrderRecord record) =>
        PaymentOrder.Restore(
            record.Id, record.UserId, (PaymentProduct)record.Product, record.ListingId, new PaymentPrice(record.Amount, record.Currency),
            (PaymentMethod)record.Method, record.PayerReference, record.CreatedAt, (PaymentOrderStatus)record.Status,
            record.DecisionNote, record.DecidedAt, record.DecidedBy);
}

internal sealed class PostgreSqlListingPromotionStore(PlatformDbContext context) : IListingPromotionStore
{
    public Task<DateTimeOffset?> FindActiveUntilAsync(Guid listingId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        context.MarketListingPromotions.AsNoTracking()
            .Where(promotion => promotion.ListingId == listingId && promotion.EndsAt > now)
            .MaxAsync(promotion => (DateTimeOffset?)promotion.EndsAt, cancellationToken);

    public async Task AddAsync(Guid listingId, Guid orderId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken = default)
    {
        var record = new MarketListingPromotionRecord { Id = Guid.NewGuid(), ListingId = listingId, OrderId = orderId, StartsAt = startsAt, EndsAt = endsAt };
        context.MarketListingPromotions.Add(record);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            context.Entry(record).State = EntityState.Detached;
        }
    }
}
