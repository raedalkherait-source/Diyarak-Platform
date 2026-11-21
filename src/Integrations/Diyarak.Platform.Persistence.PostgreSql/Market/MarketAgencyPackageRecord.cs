namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketAgencyPackageRecord
{
    public Guid Id { get; set; }

    public Guid AgencyId { get; set; }

    public int ListingQuota { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
