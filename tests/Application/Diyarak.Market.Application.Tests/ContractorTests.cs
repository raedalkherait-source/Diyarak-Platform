using Diyarak.Market.Application.Contractors;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class ContractorTests
{
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    private static SaveContractorCommand ValidCommand() => new(
        "أحمد للمقاولات",
        "بناء وترميم جميع أنواع العقارات",
        "Damascus",
        "المزة",
        ContractorSpecialty.GeneralConstruction,
        "+963912345678");

    [Fact]
    public async Task Create_succeeds()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateContractorUseCase(repo, clock);

        Result<Contractor> result = await useCase.ExecuteAsync(OwnerUserId, ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal("أحمد للمقاولات", result.Value.Name);
        Assert.False(result.Value.IsPublished);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Create_fails_duplicate()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateContractorUseCase(repo, clock);

        await useCase.ExecuteAsync(OwnerUserId, ValidCommand());
        Result<Contractor> second = await useCase.ExecuteAsync(OwnerUserId, ValidCommand());

        Assert.True(second.IsFailure);
        Assert.Equal("market.contractor.already_exists", second.Error.Code);
    }

    [Fact]
    public async Task Update_changes_name()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateContractorUseCase(repo, clock);
        var updateUseCase = new UpdateContractorUseCase(repo);

        Result<Contractor> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        var updatedCmd = ValidCommand() with { Name = "خالد للمقاولات" };

        Result<Contractor> updated = await updateUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, updatedCmd);

        Assert.True(updated.IsSuccess);
        Assert.Equal("خالد للمقاولات", updated.Value.Name);
    }

    [Fact]
    public async Task Publish_and_unpublish()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateContractorUseCase(repo, clock);
        var toggleUseCase = new ToggleContractorPublishUseCase(repo);

        Result<Contractor> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());

        Result<Contractor> published = await toggleUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, true);
        Assert.True(published.IsSuccess);
        Assert.True(repo.Items.Single().IsPublished);

        Result<Contractor> unpublished = await toggleUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 2, false);
        Assert.True(unpublished.IsSuccess);
        Assert.False(repo.Items.Single().IsPublished);
    }

    [Fact]
    public async Task Delete_fails_when_published()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateContractorUseCase(repo, clock);
        var toggleUseCase = new ToggleContractorPublishUseCase(repo);
        var deleteUseCase = new DeleteContractorUseCase(repo);

        Result<Contractor> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        await toggleUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, true);

        Result deleteResult = await deleteUseCase.ExecuteAsync(OwnerUserId, created.Value.Id);
        Assert.True(deleteResult.IsFailure);
    }

    [Fact]
    public async Task Delete_succeeds_when_unpublished()
    {
        var repo = new InMemoryContractorRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateContractorUseCase(repo, clock);
        var deleteUseCase = new DeleteContractorUseCase(repo);

        Result<Contractor> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        Result deleteResult = await deleteUseCase.ExecuteAsync(OwnerUserId, created.Value.Id);

        Assert.True(deleteResult.IsSuccess);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public void Domain_validates_name_length()
    {
        Assert.Throws<ArgumentException>(() =>
            Contractor.Create(
                Guid.NewGuid(), OwnerUserId, "x", null, "Damascus", null,
                ContractorSpecialty.GeneralConstruction, "+963912345678", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Domain_validates_specialty()
    {
        Assert.Throws<ArgumentException>(() =>
            Contractor.Create(
                Guid.NewGuid(), OwnerUserId, "Valid Name", null, "Damascus", null,
                (ContractorSpecialty)999, "+963912345678", DateTimeOffset.UtcNow));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class InMemoryContractorRepo : IContractorRepository
    {
        public List<Contractor> Items { get; } = [];

        public Task<Contractor?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.Id == id));
        public Task<Contractor?> FindByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.OwnerUserId == ownerUserId));
        public Task<IReadOnlyList<Contractor>> ListPublishedAsync(int offset, int take, string? governorate, ContractorSpecialty? specialty, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Contractor>>([.. Items
                .Where(c => c.IsPublished && (governorate == null || c.Governorate == governorate) && (!specialty.HasValue || c.Specialty == specialty.Value))
                .Skip(offset).Take(take)]);
        public Task AddAsync(Contractor contractor, CancellationToken ct = default) { Items.Add(contractor); return Task.CompletedTask; }
        public Task<bool> TrySaveAsync(Contractor contractor, long expectedVersion, CancellationToken ct = default)
        {
            int idx = Items.FindIndex(c => c.Id == contractor.Id);
            if (idx >= 0)
            {
                Items[idx] = Contractor.Restore(
                    contractor.Id, contractor.OwnerUserId, expectedVersion + 1,
                    contractor.Name, contractor.Description, contractor.Governorate, contractor.District,
                    contractor.Specialty, contractor.Phone, contractor.IsPublished, contractor.CreatedAt);
            }
            return Task.FromResult(true);
        }
        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }
    }
}
