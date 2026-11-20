namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingPhotoRecord
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public int Format { get; set; }

    public long SizeBytes { get; set; }

    public int Position { get; set; }

    public DateTimeOffset UploadedAt { get; set; }
}
