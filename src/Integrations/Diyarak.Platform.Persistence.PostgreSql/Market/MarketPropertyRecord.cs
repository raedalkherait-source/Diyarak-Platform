namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketPropertyRecord
{
    public Guid Id { get; set; }

    public int Category { get; set; }

    public string Street { get; set; } = string.Empty;

    public string HouseNumber { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public decimal? LivingAreaValue { get; set; }

    public int? LivingAreaUnit { get; set; }

    public decimal? UsableAreaValue { get; set; }

    public int? UsableAreaUnit { get; set; }

    public decimal? TotalRooms { get; set; }

    public int? BedroomCount { get; set; }

    public int? BathroomCount { get; set; }

    public int? FurnishingQuality { get; set; }

    public int[] Features { get; set; } = [];

    public int? ConstructionYear { get; set; }

    public int? LastModernizationYear { get; set; }

    public int? CommercialSubtype { get; set; }

    public decimal? SalesAreaValue { get; set; }

    public int? SalesAreaUnit { get; set; }

    public decimal? TotalAreaValue { get; set; }

    public int? TotalAreaUnit { get; set; }

    public int? ParkingSpaceCount { get; set; }
}
