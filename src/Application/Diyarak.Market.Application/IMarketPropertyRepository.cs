using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public interface IMarketPropertyRepository
{
    public Task<MarketProperty?> FindByIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<MarketProperty>> FindPageByOwnerUserIdAsync(
        Guid ownerUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IReadOnlyList<MarketProperty> properties =
            await FindByOwnerUserIdAsync(
                ownerUserId,
                cancellationToken);

        return properties
            .OrderBy(property => property.Id)
            .Skip(skip)
            .Take(take)
            .ToArray();
    }

    public Task AddAsync(
        MarketProperty property,
        CancellationToken cancellationToken = default);

    public Task<bool> TrySaveAsync(
        MarketProperty property,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}
