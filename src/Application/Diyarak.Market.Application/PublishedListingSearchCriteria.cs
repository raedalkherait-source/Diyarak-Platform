using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Market.Application;

public sealed record PublishedListingSearchCriteria(
    IReadOnlyCollection<TransactionIntent>? TransactionIntents = null,
    PublishedListingPriceSearchCriteria? Price = null,
    PublishedPropertySearchCriteria? Property = null)
{
    /// <summary>Only Listings first published strictly after this instant (saved-search counts).</summary>
    public DateTimeOffset? PublishedAfter { get; init; }

    /// <summary>Only Listings of this publisher (an agency's public page).</summary>
    public Guid? PublisherUserId { get; init; }

    /// <summary>
    /// Only Listings whose approximate (grid-snapped) position lies in this area. Matching on the
    /// snapped point, never the exact one, means narrowing the area cannot reveal a building.
    /// </summary>
    public PublishedMapArea? MapArea { get; init; }
}

public sealed record PublishedMapArea(double South, double West, double North, double East)
{
    /// <summary>Same grid as the public approximate location (ADR-0065).</summary>
    public const double GridDegrees = 0.004;

    /// <summary>Largest searchable span per side, roughly Syria's width.</summary>
    public const double MaximumSpanDegrees = 8;

    public bool IsValid =>
        double.IsFinite(South) && double.IsFinite(West) && double.IsFinite(North) && double.IsFinite(East) &&
        South is >= -90 and <= 90 && North is >= -90 and <= 90 &&
        West is >= -180 and <= 180 && East is >= -180 and <= 180 &&
        South < North && West < East &&
        North - South <= MaximumSpanDegrees && East - West <= MaximumSpanDegrees;
}

public sealed record PublishedListingPriceSearchCriteria(
    decimal? Minimum,
    decimal? Maximum,
    Currency? Currency);

public sealed record PublishedPropertySearchCriteria(
    IReadOnlyCollection<PropertyCategory>? Categories = null,
    IReadOnlyCollection<CommercialPropertySubtype>? CommercialSubtypes = null,
    PublishedPropertyLocationSearchCriteria? Location = null,
    PublishedAreaSearchCriteria? LivingArea = null,
    PublishedPropertyRoomSearchCriteria? Rooms = null,
    IReadOnlyCollection<FurnishingQuality>? FurnishingQualities = null,
    IReadOnlyCollection<PropertyFeature>? RequiredFeatures = null,
    PublishedIntegerRange? ConstructionYear = null,
    PublishedIntegerRange? ParkingSpaceCount = null,
    IReadOnlyCollection<LegalTitleType>? LegalTitleTypes = null,
    IReadOnlyCollection<FinishingLevel>? FinishingLevels = null,
    PublishedIntegerRange? FloorNumber = null);

public sealed record PublishedPropertyLocationSearchCriteria(
    string? City,
    string? PostalCode,
    IReadOnlyCollection<SyrianGovernorate>? Governorates = null,
    string? District = null);

public sealed record PublishedAreaSearchCriteria(
    decimal? Minimum,
    decimal? Maximum,
    AreaUnit? Unit);

public sealed record PublishedPropertyRoomSearchCriteria(
    PublishedDecimalRange? TotalRooms = null,
    PublishedIntegerRange? BedroomCount = null,
    PublishedIntegerRange? BathroomCount = null);

public sealed record PublishedDecimalRange(
    decimal? Minimum,
    decimal? Maximum);

public sealed record PublishedIntegerRange(
    int? Minimum,
    int? Maximum);
