using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class GetPropertyUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_identifier()
    {
        var repository = new StubMarketPropertyRepository(null);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                Guid.Empty,
                Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            GetPropertyErrors.InvalidIdentifier.Code,
            result.Error.Code);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository = new StubMarketPropertyRepository(null);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                Guid.NewGuid(),
                Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            GetPropertyErrors.InvalidActorIdentifier.Code,
            result.Error.Code);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_property_does_not_exist()
    {
        var repository = new StubMarketPropertyRepository(null);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            GetPropertyErrors.NotFound.Code,
            result.Error.Code);
        Assert.Equal(1, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_conceals_property_owned_by_another_user()
    {
        Guid ownerUserId = Guid.NewGuid();
        var property = CreateProperty(ownerUserId);
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                property.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            GetPropertyErrors.NotFound.Code,
            result.Error.Code);
        Assert.Equal(1, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_conceals_legacy_property_without_owner()
    {
        var property = CreateProperty(ownerUserId: null);
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                property.Id,
                Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(
            GetPropertyErrors.NotFound.Code,
            result.Error.Code);
        Assert.Equal(1, repository.FindCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_property_for_owner()
    {
        Guid ownerUserId = Guid.NewGuid();
        var property = CreateProperty(ownerUserId);
        var repository = new StubMarketPropertyRepository(property);
        var useCase = new GetPropertyUseCase(repository);

        Result<MarketProperty> result =
            await useCase.ExecuteAsync(
                property.Id,
                ownerUserId);

        Assert.True(result.IsSuccess);
        Assert.Same(property, result.Value);
        Assert.Equal(1, repository.FindCallCount);
    }

    private static MarketProperty CreateProperty(
        Guid? ownerUserId)
    {
        return new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.House,
            new PropertyAddress(
                "Lake Road",
                "7",
                "22301",
                "Hamburg"),
            ownerUserId: ownerUserId);
    }

    private sealed class StubMarketPropertyRepository(
        MarketProperty? existingProperty)
        : IMarketPropertyRepository
    {
        public int FindCallCount { get; private set; }

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

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
