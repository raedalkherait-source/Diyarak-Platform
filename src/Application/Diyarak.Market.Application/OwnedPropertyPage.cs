using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed record OwnedPropertyPage(
    IReadOnlyList<MarketProperty> Items,
    int Page,
    int PageSize,
    bool HasMore);
