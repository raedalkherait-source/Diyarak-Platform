namespace Diyarak.Api.Market;

internal static class MarketPaginationLinkHeader
{
    public static void Set(
        HttpResponse response,
        string collectionPath,
        int page,
        int pageSize,
        bool hasMore,
        IQueryCollection? preservedQuery = null)
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
                "prev",
                preservedQuery)
            : null;

        string? nextLink = hasMore && page < int.MaxValue
            ? CreateLink(
                collectionPath,
                page + 1,
                pageSize,
                "next",
                preservedQuery)
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
        string relation,
        IQueryCollection? preservedQuery)
    {
        string query = CreateQuery(
            page,
            pageSize,
            preservedQuery);

        return $"<{collectionPath}?{query}>; rel=\"{relation}\"";
    }

    private static string CreateQuery(
        int page,
        int pageSize,
        IQueryCollection? preservedQuery)
    {
        List<string> parameters = [];

        if (preservedQuery is not null)
        {
            foreach (var parameter in preservedQuery)
            {
                if (string.Equals(
                        parameter.Key,
                        "page",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        parameter.Key,
                        "pageSize",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string? value in parameter.Value)
                {
                    if (value is null)
                        continue;

                    parameters.Add(
                        $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(value)}");
                }
            }
        }

        parameters.Add(
            $"page={page.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        parameters.Add(
            $"pageSize={pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        return string.Join("&", parameters);
    }
}