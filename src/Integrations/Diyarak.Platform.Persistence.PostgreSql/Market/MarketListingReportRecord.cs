namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingReportRecord
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public int Reason { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int? Resolution { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public Guid? ResolvedByUserId { get; set; }
}
