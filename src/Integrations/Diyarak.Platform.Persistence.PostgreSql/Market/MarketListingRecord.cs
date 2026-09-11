namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingRecord
{
    public Guid Id { get; set; }

    public Guid PublisherUserId { get; set; }

    public Guid SubjectId { get; set; }

    public string SubjectType { get; set; } = string.Empty;

    public int Status { get; set; }

    public int? PublishingRole { get; set; }

    public int? TransactionIntent { get; set; }

    public string? Headline { get; set; }

    public bool? PriceIsOnRequest { get; set; }

    public decimal? PriceAmount { get; set; }

    public string? PriceCurrency { get; set; }

    public DateOnly? AvailableFromDate { get; set; }
}
