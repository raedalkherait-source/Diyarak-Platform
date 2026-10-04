namespace Diyarak.Market.Application;

public sealed record PublishedListingPage(
    IReadOnlyList<PublishedListingProjection> Items,
    int Page,
    int PageSize,
    bool HasMore);
