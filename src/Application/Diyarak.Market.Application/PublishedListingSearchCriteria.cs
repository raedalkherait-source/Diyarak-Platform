using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Market.Application;

public sealed record PublishedListingSearchCriteria(
    IReadOnlyCollection<TransactionIntent>? TransactionIntents = null,
    PublishedListingPriceSearchCriteria? Price = null,
    PublishedPropertySearchCriteria? Property = null);

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
    PublishedIntegerRange? ParkingSpaceCount = null);

public sealed record PublishedPropertyLocationSearchCriteria(
    string? City,
    string? PostalCode);

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
