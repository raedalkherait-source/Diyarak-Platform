namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketOffPlanProjectRecord
{
    public Guid Id { get; set; }

    public Guid AgencyId { get; set; }

    public long Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Governorate { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public int ConstructionStatus { get; set; }

    public int TotalUnits { get; set; }

    public decimal? MinPriceUsd { get; set; }

    public decimal? MaxPriceUsd { get; set; }

    public DateOnly? ExpectedCompletion { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
