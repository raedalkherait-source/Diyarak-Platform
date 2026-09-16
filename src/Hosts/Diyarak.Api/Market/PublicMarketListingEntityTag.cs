using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Diyarak.Market.Application;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Market;

internal static class PublicMarketListingEntityTag
{
    private const string DetailRepresentationRevision =
        "public-market-listing-v1";

    private const string CollectionRepresentationRevision =
        "public-market-listing-page-v1";

    internal static string Create(MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"{DetailRepresentationRevision}:{listing.Id:N}:{listing.Version}");

        return CreateOpaqueTag(material);
    }

    internal static string Create(PublishedListingPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var material = new StringBuilder();
        material.Append(CollectionRepresentationRevision);
        material.Append(':');
        material.Append(page.Page.ToString(CultureInfo.InvariantCulture));
        material.Append(':');
        material.Append(page.PageSize.ToString(CultureInfo.InvariantCulture));
        material.Append(':');
        material.Append(page.HasMore ? '1' : '0');

        foreach (MarketListing listing in page.Items)
        {
            material.Append(':');
            material.Append(listing.Id.ToString("N"));
            material.Append(':');
            material.Append(
                listing.Version.ToString(CultureInfo.InvariantCulture));
        }

        return CreateOpaqueTag(material.ToString());
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

    private static string CreateOpaqueTag(string material)
    {
        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(material));

        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
