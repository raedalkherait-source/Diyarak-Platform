using Diyarak.Platform.BuildingBlocks;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class CreatePropertyUseCase
{
    private readonly IMarketPropertyRepository _propertyRepository;

    public CreatePropertyUseCase(
        IMarketPropertyRepository propertyRepository)
    {
        ArgumentNullException.ThrowIfNull(propertyRepository);
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<Guid>> ExecuteAsync(
        CreatePropertyCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<Guid>(
                CreatePropertyErrors.InvalidActorIdentifier);
        }

        if (command is null)
        {
            return Result.Failure<Guid>(
                CreatePropertyErrors.InvalidRequest);
        }

        MarketProperty property;

        try
        {
            property = new MarketProperty(
                Guid.NewGuid(),
                command.Category,
                command.Address,
                command.LivingArea,
                command.UsableArea,
                command.TotalRooms,
                command.BedroomCount,
                command.BathroomCount,
                command.FurnishingQuality,
                command.Features,
                command.ConstructionYear,
                command.LastModernizationYear,
                command.CommercialSubtype,
                command.SalesArea,
                command.TotalArea,
                command.ParkingSpaceCount,
                actorUserId);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Guid>(
                CreatePropertyErrors.InvalidRequest);
        }

        await _propertyRepository.AddAsync(
            property,
            cancellationToken);

        return Result.Success(property.Id);
    }
}
