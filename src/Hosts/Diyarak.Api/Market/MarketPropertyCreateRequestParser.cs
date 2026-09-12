using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Api.Market;

internal static class MarketPropertyCreateRequestParser
{
    internal static bool TryParse(
        JsonElement request,
        out CreatePropertyCommand command)
    {
        command = default!;

        if (request.ValueKind != JsonValueKind.Object ||
            !TryParseEnum(
                request,
                "category",
                out PropertyCategory category) ||
            !request.TryGetProperty(
                "address",
                out JsonElement addressElement) ||
            !TryParseAddress(
                addressElement,
                out PropertyAddress? address) ||
            !TryParseOptionalArea(
                request,
                "livingArea",
                out Area? livingArea) ||
            !TryParseOptionalArea(
                request,
                "usableArea",
                out Area? usableArea) ||
            !TryParseOptionalDecimal(
                request,
                "totalRooms",
                out decimal? totalRooms) ||
            !TryParseOptionalInt32(
                request,
                "bedroomCount",
                out int? bedroomCount,
                minimum: 0) ||
            !TryParseOptionalInt32(
                request,
                "bathroomCount",
                out int? bathroomCount,
                minimum: 0) ||
            !TryParseOptionalEnum(
                request,
                "furnishingQuality",
                out FurnishingQuality? furnishingQuality) ||
            !TryParseFeatures(
                request,
                out IReadOnlyCollection<PropertyFeature>? features) ||
            !TryParseOptionalInt32(
                request,
                "constructionYear",
                out int? constructionYear,
                minimum: 1) ||
            !TryParseOptionalInt32(
                request,
                "lastModernizationYear",
                out int? lastModernizationYear,
                minimum: 1) ||
            !TryParseOptionalEnum(
                request,
                "commercialSubtype",
                out CommercialPropertySubtype? commercialSubtype) ||
            !TryParseOptionalArea(
                request,
                "salesArea",
                out Area? salesArea) ||
            !TryParseOptionalArea(
                request,
                "totalArea",
                out Area? totalArea) ||
            !TryParseOptionalInt32(
                request,
                "parkingSpaceCount",
                out int? parkingSpaceCount,
                minimum: 0))
        {
            return false;
        }

        command = new CreatePropertyCommand(
            category,
            address!,
            livingArea,
            usableArea,
            totalRooms,
            bedroomCount,
            bathroomCount,
            furnishingQuality,
            features,
            constructionYear,
            lastModernizationYear,
            commercialSubtype,
            salesArea,
            totalArea,
            parkingSpaceCount);

        return true;
    }

    private static bool TryParseAddress(
        JsonElement element,
        out PropertyAddress? address)
    {
        address = null;

        if (element.ValueKind != JsonValueKind.Object ||
            !TryGetRequiredString(
                element,
                "street",
                out string? street) ||
            !TryGetRequiredString(
                element,
                "houseNumber",
                out string? houseNumber) ||
            !TryGetRequiredString(
                element,
                "postalCode",
                out string? postalCode) ||
            !TryGetRequiredString(
                element,
                "city",
                out string? city) ||
            !TryParseOptionalLocation(
                element,
                out GeoCoordinate? location))
        {
            return false;
        }

        try
        {
            address = new PropertyAddress(
                street!,
                houseNumber!,
                postalCode!,
                city!,
                location);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseOptionalLocation(
        JsonElement parent,
        out GeoCoordinate? location)
    {
        location = null;

        if (!parent.TryGetProperty(
                "location",
                out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                "latitude",
                out JsonElement latitudeElement) ||
            !latitudeElement.TryGetDouble(out double latitude) ||
            !element.TryGetProperty(
                "longitude",
                out JsonElement longitudeElement) ||
            !longitudeElement.TryGetDouble(out double longitude))
        {
            return false;
        }

        try
        {
            location = new GeoCoordinate(latitude, longitude);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryParseOptionalArea(
        JsonElement parent,
        string propertyName,
        out Area? area)
    {
        area = null;

        if (!parent.TryGetProperty(
                propertyName,
                out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                "value",
                out JsonElement valueElement) ||
            !valueElement.TryGetDecimal(out decimal value) ||
            value < 0m ||
            !TryParseEnum(
                element,
                "unit",
                out AreaUnit unit))
        {
            return false;
        }

        area = new Area(value, unit);
        return true;
    }

    private static bool TryParseFeatures(
        JsonElement parent,
        out IReadOnlyCollection<PropertyFeature>? features)
    {
        features = null;

        if (!parent.TryGetProperty(
                "features",
                out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Array)
            return false;

        var parsed = new List<PropertyFeature>();

        foreach (JsonElement featureElement in element.EnumerateArray())
        {
            if (featureElement.ValueKind != JsonValueKind.String ||
                !Enum.TryParse(
                    featureElement.GetString(),
                    ignoreCase: true,
                    out PropertyFeature feature) ||
                !Enum.IsDefined(feature))
            {
                return false;
            }

            parsed.Add(feature);
        }

        features = parsed;
        return true;
    }

    private static bool TryParseOptionalDecimal(
        JsonElement parent,
        string propertyName,
        out decimal? value)
    {
        value = null;

        if (!parent.TryGetProperty(
                propertyName,
                out JsonElement element))
        {
            return true;
        }

        if (!element.TryGetDecimal(out decimal parsed) ||
            parsed < 0m)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryParseOptionalInt32(
        JsonElement parent,
        string propertyName,
        out int? value,
        int minimum)
    {
        value = null;

        if (!parent.TryGetProperty(
                propertyName,
                out JsonElement element))
        {
            return true;
        }

        if (!element.TryGetInt32(out int parsed) ||
            parsed < minimum)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetRequiredString(
        JsonElement parent,
        string propertyName,
        out string? value)
    {
        value = null;

        if (!parent.TryGetProperty(
                propertyName,
                out JsonElement element) ||
            element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseEnum<TEnum>(
        JsonElement parent,
        string propertyName,
        out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;

        return parent.TryGetProperty(
                   propertyName,
                   out JsonElement element) &&
               element.ValueKind == JsonValueKind.String &&
               Enum.TryParse(
                   element.GetString(),
                   ignoreCase: true,
                   out value) &&
               Enum.IsDefined(value);
    }

    private static bool TryParseOptionalEnum<TEnum>(
        JsonElement parent,
        string propertyName,
        out TEnum? value)
        where TEnum : struct, Enum
    {
        value = null;

        if (!parent.TryGetProperty(
                propertyName,
                out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.String ||
            !Enum.TryParse(
                element.GetString(),
                ignoreCase: true,
                out TEnum parsed) ||
            !Enum.IsDefined(parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
