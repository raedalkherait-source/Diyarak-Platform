using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Market;

internal static class PublicMarketListingEntityTag
{
    private const string RepresentationRevision = "public-market-listing-v1";

    internal static string Create(MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"{RepresentationRevision}:{listing.Id:N}:{listing.Version}");

        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(material));

        return $"\"{Convert.ToHexString(hash)}\"";
    }

    internal static bool MatchesIfNoneMatch(
        IHeaderDictionary headers,
        string currentEntityTag)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEntityTag);

        if (!headers.TryGetValue(
                "If-None-Match",
                out var headerValues))
        {
            return false;
        }

        foreach (string? headerValue in headerValues)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                continue;

            string[] candidates = headerValue.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (string rawCandidate in candidates)
            {
                if (rawCandidate == "*")
                    return true;

                string candidate = rawCandidate.StartsWith(
                    "W/",
                    StringComparison.OrdinalIgnoreCase)
                    ? rawCandidate[2..].TrimStart()
                    : rawCandidate;

                if (string.Equals(
                        candidate,
                        currentEntityTag,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
