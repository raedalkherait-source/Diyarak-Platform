using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Xunit;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class ListOwnedPropertiesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_validation_failure_for_empty_actor_identifier()
    {
        var repository = new StubMarketPropertyRepository([]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<IReadOnlyList<MarketProperty>> result =
            await useCase.ExecuteAsync(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ListOwnedPropertiesErrors.InvalidActorIdentifier,
            result.Error);
        Assert.Equal(0, repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_collection_when_actor_has_no_properties()
    {
        var repository = new StubMarketPropertyRepository([]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<IReadOnlyList<MarketProperty>> result =
            await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_repository_properties_for_actor()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty first = CreateProperty(actorUserId);
        MarketProperty second = CreateProperty(actorUserId);
        var repository = new StubMarketPropertyRepository(
            [first, second]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<IReadOnlyList<MarketProperty>> result =
            await useCase.ExecuteAsync(actorUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(first, result.Value);
        Assert.Contains(second, result.Value);
        Assert.Equal(1, repository.FindByOwnerCallCount);
        Assert.Equal(actorUserId, repository.LastOwnerUserId);
    }

    private static MarketProperty CreateProperty(Guid ownerUserId) =>
        new(
            Guid.NewGuid(),
            PropertyCategory.House,
            new PropertyAddress(
                "Lake Road",
                "7",
                "22301",
                "Hamburg"),
            ownerUserId: ownerUserId);

    private sealed class StubMarketPropertyRepository(
        IReadOnlyList<MarketProperty> properties)
        : IMarketPropertyRepository
    {
        public int FindByOwnerCallCount { get; private set; }

        public Guid? LastOwnerUserId { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(null);

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default)
        {
            FindByOwnerCallCount++;
            LastOwnerUserId = ownerUserId;
            return Task.FromResult(properties);
        }

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
