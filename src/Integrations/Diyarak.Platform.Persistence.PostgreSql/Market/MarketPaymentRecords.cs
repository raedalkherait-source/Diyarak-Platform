namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketPaymentOrderRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Product { get; set; }
    public Guid ListingId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Method { get; set; }
    public string PayerReference { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int Status { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
}

internal sealed class MarketListingPromotionRecord
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid OrderId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}
