using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Diyarak.Market.Application;

namespace Diyarak.Api.Market;

internal static class PublicMarketListingEntityTag
{
    private const string DetailRepresentationRevision =
        "public-market-listing-v2";

    private const string CollectionRepresentationRevision =
        "public-market-listing-page-v2";

    internal static string Create(PublishedListingProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"{DetailRepresentationRevision}:{projection.Listing.Id:N}:{projection.Listing.Version}:{projection.Property.Version}");

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

        foreach (PublishedListingProjection projection in page.Items)
        {
            material.Append(':');
            material.Append(projection.Listing.Id.ToString("N"));
            material.Append(':');
            material.Append(
                projection.Listing.Version.ToString(CultureInfo.InvariantCulture));
            material.Append(':');
            material.Append(
                projection.Property.Version.ToString(CultureInfo.InvariantCulture));
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
