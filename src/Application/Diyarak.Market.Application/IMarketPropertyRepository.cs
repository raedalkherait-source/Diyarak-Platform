using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public interface IMarketPropertyRepository
{
    public Task AddAsync(
        MarketProperty property,
        CancellationToken cancellationToken = default);
}
