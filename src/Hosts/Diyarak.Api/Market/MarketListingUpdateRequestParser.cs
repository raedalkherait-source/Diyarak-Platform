using System.Globalization;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Api.Market;

internal static class MarketListingUpdateRequestParser
{
    internal static bool TryParse(
        JsonElement request,
        out UpdateListingPatch patch)
    {
        patch = default!;

        if (request.ValueKind != JsonValueKind.Object ||
            !request.TryGetProperty("version", out JsonElement versionElement) ||
            !versionElement.TryGetInt64(out long version) ||
            version <= 0)
        {
            return false;
        }

        bool updateContext = request.TryGetProperty(
            "context",
            out JsonElement contextElement);
        bool updateHeadline = request.TryGetProperty(
            "headline",
            out JsonElement headlineElement);
        bool updatePrice = request.TryGetProperty(
            "price",
            out JsonElement priceElement);
        bool updateAvailableFromDate = request.TryGetProperty(
            "availableFromDate",
            out JsonElement availableFromDateElement);

        ListingContext? context = null;
        ListingHeadline? headline = null;
        ListingPrice? price = null;
        ListingAvailableFromDate? availableFromDate = null;

        if (updateContext &&
            !TryParseContext(contextElement, out context))
        {
            return false;
        }

        if (updateHeadline &&
            !TryParseHeadline(headlineElement, out headline))
        {
            return false;
        }

        if (updatePrice &&
            !TryParsePrice(priceElement, out price))
        {
            return false;
        }

        if (updateAvailableFromDate &&
            !TryParseAvailableFromDate(
                availableFromDateElement,
                out availableFromDate))
        {
            return false;
        }

        patch = new UpdateListingPatch(
            version,
            updateContext,
            context,
            updateHeadline,
            headline,
            updatePrice,
            price,
            updateAvailableFromDate,
            availableFromDate);

        return true;
    }

    private static bool TryParseContext(
        JsonElement element,
        out ListingContext? context)
    {
        context = null;

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                "publishingRole",
                out JsonElement publishingRoleElement) ||
            publishingRoleElement.ValueKind != JsonValueKind.String ||
            !Enum.TryParse(
                publishingRoleElement.GetString(),
                ignoreCase: true,
                out PublishingRole publishingRole) ||
            !Enum.IsDefined(publishingRole) ||
            !element.TryGetProperty(
                "transactionIntent",
                out JsonElement transactionIntentElement) ||
            transactionIntentElement.ValueKind != JsonValueKind.String ||
            !Enum.TryParse(
                transactionIntentElement.GetString(),
                ignoreCase: true,
                out TransactionIntent transactionIntent) ||
            !Enum.IsDefined(transactionIntent))
        {
            return false;
        }

        context = new ListingContext(
            publishingRole,
            transactionIntent);

        return true;
    }

    private static bool TryParseHeadline(
        JsonElement element,
        out ListingHeadline? headline)
    {
        headline = null;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        string? value = element.GetString();

        if (string.IsNullOrWhiteSpace(value))
            return false;

        headline = new ListingHeadline(value);
        return true;
    }

    private static bool TryParsePrice(
        JsonElement element,
        out ListingPrice? price)
    {
        price = null;

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                "isOnRequest",
                out JsonElement isOnRequestElement) ||
            (isOnRequestElement.ValueKind != JsonValueKind.True &&
             isOnRequestElement.ValueKind != JsonValueKind.False))
        {
            return false;
        }

        bool isOnRequest = isOnRequestElement.GetBoolean();
        bool hasAmount = element.TryGetProperty(
            "amount",
            out JsonElement amountElement);
        bool hasCurrency = element.TryGetProperty(
            "currency",
            out JsonElement currencyElement);

        if (isOnRequest)
        {
            if (hasAmount || hasCurrency)
                return false;

            price = ListingPrice.OnRequest();
            return true;
        }

        if (!hasAmount ||
            !amountElement.TryGetDecimal(out decimal amount) ||
            amount < 0m ||
            !hasCurrency ||
            currencyElement.ValueKind != JsonValueKind.String ||
            !Currency.TryCreate(
                currencyElement.GetString(),
                out Currency? currency))
        {
            return false;
        }

        price = ListingPrice.Known(
            new Money(amount, currency));

        return true;
    }

    private static bool TryParseAvailableFromDate(
        JsonElement element,
        out ListingAvailableFromDate? availableFromDate)
    {
        availableFromDate = null;

        if (element.ValueKind == JsonValueKind.Null)
            return true;

        if (element.ValueKind != JsonValueKind.String ||
            !DateOnly.TryParseExact(
                element.GetString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            return false;
        }

        availableFromDate = new ListingAvailableFromDate(date);
        return true;
    }
}
