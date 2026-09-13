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
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (propertyId == Guid.Empty)
        {
            return Result.Failure<MarketProperty>(
                GetPropertyErrors.InvalidIdentifier);
        }

        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<MarketProperty>(
                GetPropertyErrors.InvalidActorIdentifier);
        }

        MarketProperty? property =
            await _propertyRepository.FindByIdAsync(
                propertyId,
                cancellationToken);

        if (property is null ||
            property.OwnerUserId != actorUserId)
        {
            return Result.Failure<MarketProperty>(
                GetPropertyErrors.NotFound);
        }

        return Result.Success(property);
    }
}
