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

        Result<OwnedPropertyPage> result =
            await useCase.ExecuteAsync(Guid.Empty, 1, 20);

        Assert.True(result.IsFailure);
        Assert.Equal(ListOwnedPropertiesErrors.InvalidActorIdentifier, result.Error);
        Assert.Equal(0, repository.FindByOwnerCallCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ExecuteAsync_returns_validation_failure_for_invalid_pagination(
        int page,
        int pageSize)
    {
        var repository = new StubMarketPropertyRepository([]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<OwnedPropertyPage> result =
            await useCase.ExecuteAsync(Guid.NewGuid(), page, pageSize);

        Assert.True(result.IsFailure);
        Assert.Equal(ListOwnedPropertiesErrors.InvalidPagination, result.Error);
        Assert.Equal(0, repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_bounded_ordered_page_and_has_more()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty third = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000003"), actorUserId);
        MarketProperty first = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), actorUserId);
        MarketProperty second = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000002"), actorUserId);
        var repository = new StubMarketPropertyRepository([third, first, second]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<OwnedPropertyPage> result =
            await useCase.ExecuteAsync(actorUserId, 1, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.True(result.Value.HasMore);
        Assert.Equal(new[] { first.Id, second.Id }, result.Value.Items.Select(item => item.Id));
        Assert.Equal(1, repository.FindByOwnerCallCount);
        Assert.Equal(actorUserId, repository.LastOwnerUserId);
    }

    [Fact]
    public async Task ExecuteAsync_returns_second_page_without_has_more()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty first = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), actorUserId);
        MarketProperty second = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000002"), actorUserId);
        MarketProperty third = CreateProperty(
            Guid.Parse("00000000-0000-0000-0000-000000000003"), actorUserId);
        var repository = new StubMarketPropertyRepository([first, second, third]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        Result<OwnedPropertyPage> result =
            await useCase.ExecuteAsync(actorUserId, 2, 2);

        Assert.True(result.IsSuccess);
        MarketProperty item = Assert.Single(result.Value.Items);
        Assert.Equal(third.Id, item.Id);
        Assert.False(result.Value.HasMore);
    }

    [Fact]
    public async Task ExecuteAsync_fails_closed_when_repository_returns_other_owner()
    {
        Guid actorUserId = Guid.NewGuid();
        var repository = new StubMarketPropertyRepository(
            [CreateProperty(Guid.NewGuid(), Guid.NewGuid())]);
        var useCase = new ListOwnedPropertiesUseCase(repository);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.ExecuteAsync(actorUserId, 1, 20));

        Assert.Contains("another owner", exception.Message);
    }

    private static MarketProperty CreateProperty(
        Guid id,
        Guid ownerUserId) =>
        new(
            id,
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

        public Task<bool> TrySaveAsync(
            MarketProperty property,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
