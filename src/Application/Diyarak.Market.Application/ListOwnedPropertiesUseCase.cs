using Diyarak.Platform.BuildingBlocks;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class ListOwnedPropertiesUseCase
{
    private readonly IMarketPropertyRepository _propertyRepository;

    public ListOwnedPropertiesUseCase(
        IMarketPropertyRepository propertyRepository)
    {
        ArgumentNullException.ThrowIfNull(propertyRepository);
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<IReadOnlyList<MarketProperty>>> ExecuteAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<MarketProperty>>(
                ListOwnedPropertiesErrors.InvalidActorIdentifier);
        }

        IReadOnlyList<MarketProperty> properties =
            await _propertyRepository.FindByOwnerUserIdAsync(
                actorUserId,
                cancellationToken);

        return Result.Success(properties);
    }
}
