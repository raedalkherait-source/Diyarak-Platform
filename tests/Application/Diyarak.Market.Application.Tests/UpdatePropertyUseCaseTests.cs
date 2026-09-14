using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class UpdatePropertyUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_property_identifier()
    {
        var repository = new StubMarketPropertyRepository(null);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            Guid.Empty,
            Guid.NewGuid(),
            CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.InvalidIdentifier.Code, result.Error.Code);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_invalid_version()
    {
        var repository = new StubMarketPropertyRepository(null);
        var useCase = new UpdatePropertyUseCase(repository);
        UpdatePropertyCommand command = CreateCommand() with { ExpectedVersion = 0 };

        Result<long> result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            command);

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.InvalidRequest.Code, result.Error.Code);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_conceals_property_owned_by_another_user()
    {
        MarketProperty property = CreateProperty(Guid.NewGuid());
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            property.Id,
            Guid.NewGuid(),
            CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.NotFound.Code, result.Error.Code);
        Assert.Equal(0, repository.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_conceals_legacy_property_without_owner()
    {
        MarketProperty property = CreateProperty(null);
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            property.Id,
            Guid.NewGuid(),
            CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.NotFound.Code, result.Error.Code);
        Assert.Equal(0, repository.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_conflict_for_stale_version()
    {
        Guid ownerUserId = Guid.NewGuid();
        MarketProperty property = MarketProperty.Restore(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress("Original Street"),
            livingArea: null,
            usableArea: null,
            totalRooms: null,
            bedroomCount: null,
            bathroomCount: null,
            furnishingQuality: null,
            features: null,
            constructionYear: null,
            lastModernizationYear: null,
            commercialSubtype: null,
            salesArea: null,
            totalArea: null,
            parkingSpaceCount: null,
            ownerUserId: ownerUserId,
            version: 3);
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            property.Id,
            ownerUserId,
            CreateCommand(expectedVersion: 2));

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.ConcurrentModification.Code, result.Error.Code);
        Assert.Equal(0, repository.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_replaces_details_preserves_identity_and_returns_next_version()
    {
        Guid ownerUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(ownerUserId);
        var repository = new StubMarketPropertyRepository(property, saveResult: true);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            property.Id,
            ownerUserId,
            CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(1, repository.SaveCallCount);
        Assert.Equal(1, repository.LastExpectedVersion);
        Assert.Same(property, repository.LastSavedProperty);
        Assert.Equal(ownerUserId, property.OwnerUserId);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal("Replacement Street", property.Address.Street);
        Assert.Equal(82m, property.LivingArea?.Value);
        Assert.Null(property.UsableArea);
        Assert.Equal(2, property.Features.Count);
    }

    [Fact]
    public async Task ExecuteAsync_maps_persistence_concurrency_failure_to_conflict()
    {
        Guid ownerUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(ownerUserId);
        var repository = new StubMarketPropertyRepository(property, saveResult: false);
        var useCase = new UpdatePropertyUseCase(repository);

        Result<long> result = await useCase.ExecuteAsync(
            property.Id,
            ownerUserId,
            CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(UpdatePropertyErrors.ConcurrentModification.Code, result.Error.Code);
        Assert.Equal(1, repository.SaveCallCount);
    }

    private static UpdatePropertyCommand CreateCommand(long expectedVersion = 1) =>
        new(
            expectedVersion,
            new CreatePropertyCommand(
                PropertyCategory.Apartment,
                CreateAddress("Replacement Street"),
                LivingArea: new Area(82m, AreaUnit.SquareMeter),
                Features:
                [
                    PropertyFeature.FittedKitchen,
                    PropertyFeature.BalconyOrTerrace,
                ]));

    private static MarketProperty CreateProperty(Guid? ownerUserId) =>
        new(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress("Original Street"),
            usableArea: new Area(100m, AreaUnit.SquareMeter),
            ownerUserId: ownerUserId);

    private static PropertyAddress CreateAddress(string street) =>
        new(street, "1", "23552", "Luebeck");

    private sealed class StubMarketPropertyRepository(
        MarketProperty? existingProperty,
        bool saveResult = false)
        : IMarketPropertyRepository
    {
        public int FindCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public long? LastExpectedVersion { get; private set; }

        public MarketProperty? LastSavedProperty { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;
            return Task.FromResult(
                existingProperty?.Id == propertyId
                    ? existingProperty
                    : null);
        }

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([]);

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(
            MarketProperty property,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            LastExpectedVersion = expectedVersion;
            LastSavedProperty = property;
            return Task.FromResult(saveResult);
        }
    }
}
