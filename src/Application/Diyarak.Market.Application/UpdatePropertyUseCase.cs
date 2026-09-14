using Diyarak.Platform.BuildingBlocks;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class UpdatePropertyUseCase
{
    private readonly IMarketPropertyRepository _propertyRepository;

    public UpdatePropertyUseCase(
        IMarketPropertyRepository propertyRepository)
    {
        ArgumentNullException.ThrowIfNull(propertyRepository);
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<long>> ExecuteAsync(
        Guid propertyId,
        Guid actorUserId,
        UpdatePropertyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (propertyId == Guid.Empty)
            return Result.Failure<long>(UpdatePropertyErrors.InvalidIdentifier);

        if (actorUserId == Guid.Empty)
            return Result.Failure<long>(UpdatePropertyErrors.InvalidActorIdentifier);

        if (command is null ||
            command.ExpectedVersion <= 0 ||
            command.Replacement is null)
        {
            return Result.Failure<long>(UpdatePropertyErrors.InvalidRequest);
        }

        MarketProperty? property =
            await _propertyRepository.FindByIdAsync(
                propertyId,
                cancellationToken);

        if (property is null ||
            property.OwnerUserId is null ||
            property.OwnerUserId != actorUserId)
        {
            return Result.Failure<long>(UpdatePropertyErrors.NotFound);
        }

        if (property.Version != command.ExpectedVersion)
        {
            return Result.Failure<long>(
                UpdatePropertyErrors.ConcurrentModification);
        }

        CreatePropertyCommand replacement = command.Replacement;

        try
        {
            property.ReplaceDetails(
                replacement.Category,
                replacement.Address,
                replacement.LivingArea,
                replacement.UsableArea,
                replacement.TotalRooms,
                replacement.BedroomCount,
                replacement.BathroomCount,
                replacement.FurnishingQuality,
                replacement.Features,
                replacement.ConstructionYear,
                replacement.LastModernizationYear,
                replacement.CommercialSubtype,
                replacement.SalesArea,
                replacement.TotalArea,
                replacement.ParkingSpaceCount);
        }
        catch (ArgumentException)
        {
            return Result.Failure<long>(UpdatePropertyErrors.InvalidRequest);
        }

        bool saved =
            await _propertyRepository.TrySaveAsync(
                property,
                command.ExpectedVersion,
                cancellationToken);

        if (!saved)
        {
            return Result.Failure<long>(
                UpdatePropertyErrors.ConcurrentModification);
        }

        return Result.Success(checked(command.ExpectedVersion + 1));
    }
}
