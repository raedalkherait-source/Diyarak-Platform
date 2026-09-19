namespace Diyarak.Api.Market;

internal static class MarketPaginationLinkHeader
{
    public static void Set(
        HttpResponse response,
        string collectionPath,
        int page,
        int pageSize,
        bool hasMore)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        string? previousLink = page > 1
            ? CreateLink(
                collectionPath,
                page - 1,
                pageSize,
                "prev")
            : null;

        string? nextLink = hasMore && page < int.MaxValue
            ? CreateLink(
                collectionPath,
                page + 1,
                pageSize,
                "next")
            : null;

        if (previousLink is not null && nextLink is not null)
        {
            response.Headers["Link"] =
                $"{previousLink}, {nextLink}";
            return;
        }

        if (previousLink is not null)
        {
            response.Headers["Link"] = previousLink;
            return;
        }

        if (nextLink is not null)
            response.Headers["Link"] = nextLink;
    }

    private static string CreateLink(
        string collectionPath,
        int page,
        int pageSize,
        string relation)
    {
        return $"<{collectionPath}?page={page}&pageSize={pageSize}>; rel=\"{relation}\"";
    }
}
