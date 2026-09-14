using System.Text.Json;
using Diyarak.Market.Application;

namespace Diyarak.Api.Market;

internal static class MarketPropertyUpdateRequestParser
{
    internal static bool TryParse(
        JsonElement request,
        out UpdatePropertyCommand command)
    {
        command = default!;

        if (request.ValueKind != JsonValueKind.Object ||
            !request.TryGetProperty("version", out JsonElement versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt64(out long version) ||
            version <= 0 ||
            !MarketPropertyCreateRequestParser.TryParse(
                request,
                out CreatePropertyCommand replacement))
        {
            return false;
        }

        command = new UpdatePropertyCommand(
            version,
            replacement);

        return true;
    }
}
