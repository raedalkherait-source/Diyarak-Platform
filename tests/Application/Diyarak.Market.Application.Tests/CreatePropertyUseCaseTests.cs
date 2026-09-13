using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class CreatePropertyUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_creates_actor_owned_property_and_persists_it()
    {
        Guid actorUserId = Guid.NewGuid();
        var repository = new StubMarketPropertyRepository();
        var useCase = new CreatePropertyUseCase(repository);

        var command = new CreatePropertyCommand(
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Market Street",
                "12A",
                "23552",
                "Luebeck",
                new GeoCoordinate(53.8655, 10.6866)),
            LivingArea: new Area(82.5m, AreaUnit.SquareMeter),
            UsableArea: new Area(91m, AreaUnit.SquareMeter),
            TotalRooms: 3.5m,
            BedroomCount: 2,
            BathroomCount: 1,
            FurnishingQuality: FurnishingQuality.Upscale,
            Features:
            [
                PropertyFeature.FittedKitchen,
                PropertyFeature.BalconyOrTerrace,
            ],
            ConstructionYear: 1998,
            LastModernizationYear: 2024,
            ParkingSpaceCount: 1);

        Result<Guid> result = await useCase.ExecuteAsync(
            command,
            actorUserId);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        MarketProperty property = Assert.IsType<MarketProperty>(
            repository.AddedProperty);

        Assert.Equal(result.Value, property.Id);
        Assert.Equal(actorUserId, property.OwnerUserId);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal("Market Street", property.Address.Street);
        Assert.Equal("Luebeck", property.Address.City);
        Assert.Equal(82.5m, property.LivingArea?.Value);
        Assert.Equal(2, property.BedroomCount);
        Assert.Contains(
            PropertyFeature.BalconyOrTerrace,
            property.Features);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository = new StubMarketPropertyRepository();
        var useCase = new CreatePropertyUseCase(repository);

        var command = new CreatePropertyCommand(
            PropertyCategory.House,
            new PropertyAddress(
                "Market Street",
                "12A",
                "23552",
                "Luebeck"));

        Result<Guid> result = await useCase.ExecuteAsync(
            command,
            Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreatePropertyErrors.InvalidActorIdentifier.Code,
            result.Error.Code);
        Assert.Null(repository.AddedProperty);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_invalid_domain_combination()
    {
        var repository = new StubMarketPropertyRepository();
        var useCase = new CreatePropertyUseCase(repository);

        var command = new CreatePropertyCommand(
            PropertyCategory.House,
            new PropertyAddress(
                "Market Street",
                "12A",
                "23552",
                "Luebeck"),
            CommercialSubtype: CommercialPropertySubtype.OfficeOrPractice);

        Result<Guid> result = await useCase.ExecuteAsync(
            command,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CreatePropertyErrors.InvalidRequest.Code,
            result.Error.Code);
        Assert.Null(repository.AddedProperty);
    }

    private sealed class StubMarketPropertyRepository
        : IMarketPropertyRepository
    {
        public MarketProperty? AddedProperty { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(null);

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default)
        {
            AddedProperty = property;
            return Task.CompletedTask;
        }
    }
}
