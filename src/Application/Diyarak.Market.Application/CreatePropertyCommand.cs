using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Market.Application;

public sealed record CreatePropertyCommand(
    PropertyCategory Category,
    PropertyAddress Address,
    Area? LivingArea = null,
    Area? UsableArea = null,
    decimal? TotalRooms = null,
    int? BedroomCount = null,
    int? BathroomCount = null,
    FurnishingQuality? FurnishingQuality = null,
    IReadOnlyCollection<PropertyFeature>? Features = null,
    int? ConstructionYear = null,
    int? LastModernizationYear = null,
    CommercialPropertySubtype? CommercialSubtype = null,
    Area? SalesArea = null,
    Area? TotalArea = null,
    int? ParkingSpaceCount = null);
