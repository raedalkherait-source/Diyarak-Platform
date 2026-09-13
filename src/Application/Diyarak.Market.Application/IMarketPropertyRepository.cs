using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public interface IMarketPropertyRepository
{
    public Task<MarketProperty?> FindByIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    public Task AddAsync(
        MarketProperty property,
        CancellationToken cancellationToken = default);
}
