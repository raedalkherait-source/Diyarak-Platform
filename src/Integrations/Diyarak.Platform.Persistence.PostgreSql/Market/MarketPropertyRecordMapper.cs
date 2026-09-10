using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal static class MarketPropertyRecordMapper
{
    internal static MarketPropertyRecord FromDomain(
        MarketProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return new MarketPropertyRecord
        {
            Id = property.Id,
            Category = (int)property.Category,
            Street = property.Address.Street,
            HouseNumber = property.Address.HouseNumber,
            PostalCode = property.Address.PostalCode,
            City = property.Address.City,
            Latitude = property.Address.Location?.Latitude,
            Longitude = property.Address.Location?.Longitude,
            LivingAreaValue = property.LivingArea?.Value,
            LivingAreaUnit = property.LivingArea is { } livingArea
                ? (int)livingArea.Unit
                : null,
            UsableAreaValue = property.UsableArea?.Value,
            UsableAreaUnit = property.UsableArea is { } usableArea
                ? (int)usableArea.Unit
                : null,
            TotalRooms = property.TotalRooms,
            BedroomCount = property.BedroomCount,
            BathroomCount = property.BathroomCount,
            FurnishingQuality =
                property.FurnishingQuality is { } furnishingQuality
                    ? (int)furnishingQuality
                    : null,
            Features = property.Features
                .Select(static feature => (int)feature)
                .ToArray(),
            ConstructionYear = property.ConstructionYear,
            LastModernizationYear = property.LastModernizationYear,
            CommercialSubtype =
                property.CommercialSubtype is { } commercialSubtype
                    ? (int)commercialSubtype
                    : null,
            SalesAreaValue = property.SalesArea?.Value,
            SalesAreaUnit = property.SalesArea is { } salesArea
                ? (int)salesArea.Unit
                : null,
            TotalAreaValue = property.TotalArea?.Value,
            TotalAreaUnit = property.TotalArea is { } totalArea
                ? (int)totalArea.Unit
                : null,
            ParkingSpaceCount = property.ParkingSpaceCount,
        };
    }

    internal static MarketProperty ToDomain(
        MarketPropertyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        GeoCoordinate? location =
            CreateLocation(record.Latitude, record.Longitude);

        return new MarketProperty(
            record.Id,
            (PropertyCategory)record.Category,
            new PropertyAddress(
                record.Street,
                record.HouseNumber,
                record.PostalCode,
                record.City,
                location),
            livingArea: CreateArea(
                record.LivingAreaValue,
                record.LivingAreaUnit,
                "living area"),
            usableArea: CreateArea(
                record.UsableAreaValue,
                record.UsableAreaUnit,
                "usable area"),
            totalRooms: record.TotalRooms,
            bedroomCount: record.BedroomCount,
            bathroomCount: record.BathroomCount,
            furnishingQuality:
                record.FurnishingQuality is { } furnishingQuality
                    ? (FurnishingQuality)furnishingQuality
                    : null,
            features: record.Features.Select(
                static feature => (PropertyFeature)feature),
            constructionYear: record.ConstructionYear,
            lastModernizationYear: record.LastModernizationYear,
            commercialSubtype:
                record.CommercialSubtype is { } commercialSubtype
                    ? (CommercialPropertySubtype)commercialSubtype
                    : null,
            salesArea: CreateArea(
                record.SalesAreaValue,
                record.SalesAreaUnit,
                "sales area"),
            totalArea: CreateArea(
                record.TotalAreaValue,
                record.TotalAreaUnit,
                "total area"),
            parkingSpaceCount: record.ParkingSpaceCount);
    }

    private static GeoCoordinate? CreateLocation(
        double? latitude,
        double? longitude)
    {
        if (latitude is null && longitude is null)
            return null;

        if (latitude is null || longitude is null)
            throw new InvalidOperationException(
                "Persisted Property location must contain both latitude and longitude.");

        return new GeoCoordinate(
            latitude.Value,
            longitude.Value);
    }

    private static Area? CreateArea(
        decimal? value,
        int? unit,
        string areaName)
    {
        if (value is null && unit is null)
            return null;

        if (value is null || unit is null)
            throw new InvalidOperationException(
                $"Persisted Property {areaName} must contain both value and unit.");

        return new Area(
            value.Value,
            (AreaUnit)unit.Value);
    }
}
