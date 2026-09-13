using Diyarak.Platform.BuildingBlocks;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class GetPropertyUseCase
{
    private readonly IMarketPropertyRepository _propertyRepository;

    public GetPropertyUseCase(
        IMarketPropertyRepository propertyRepository)
    {
        ArgumentNullException.ThrowIfNull(propertyRepository);
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<MarketProperty>> ExecuteAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        if (propertyId == Guid.Empty)
        {
            return Result.Failure<MarketProperty>(
                GetPropertyErrors.InvalidIdentifier);
        }

        MarketProperty? property =
            await _propertyRepository.FindByIdAsync(
                propertyId,
                cancellationToken);

        return property is null
            ? Result.Failure<MarketProperty>(
                GetPropertyErrors.NotFound)
            : Result.Success(property);
    }
}
