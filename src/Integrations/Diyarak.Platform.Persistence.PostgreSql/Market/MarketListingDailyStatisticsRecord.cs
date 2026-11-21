namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingDailyStatisticsRecord
{
    public Guid ListingId { get; set; }

    public DateOnly Day { get; set; }

    public int Views { get; set; }

    public int ContactReveals { get; set; }
}
