namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketContractorRecord
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public long Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Governorate { get; set; } = string.Empty;

    public string? District { get; set; }

    public int Specialty { get; set; }

    public string Phone { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
