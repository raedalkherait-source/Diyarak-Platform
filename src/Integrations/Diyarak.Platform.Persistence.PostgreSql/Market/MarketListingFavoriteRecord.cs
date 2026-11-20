namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingFavoriteRecord
{
    public Guid UserId { get; set; }

    public Guid ListingId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
