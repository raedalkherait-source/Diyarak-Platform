using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Application.OffPlanProjects;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class OffPlanProjectTests
{
    private static readonly Guid AgencyId = Guid.NewGuid();
    private static readonly Guid OwnerUserId = Guid.NewGuid();
    private static readonly Agency TestAgency = Agency.Create(AgencyId, OwnerUserId, "مكتب النور", null, "Damascus", "دمشق");

    private static SaveOffPlanProjectCommand ValidCommand() => new(
        "برج السلام",
        "مشروع سكني فاخر",
        "Damascus",
        "المزة",
        ConstructionStatus.UnderConstruction,
        50,
        80_000m,
        200_000m,
        new DateOnly(2028, 6, 1));

    [Fact]
    public async Task Create_succeeds_for_agency_owner()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);

        Result<OffPlanProject> result = await useCase.ExecuteAsync(OwnerUserId, ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal("برج السلام", result.Value.Name);
        Assert.Equal(AgencyId, result.Value.AgencyId);
        Assert.False(result.Value.IsPublished);
        Assert.Single(projects.Items);
    }

    [Fact]
    public async Task Create_fails_for_non_agency_user()
    {
        var agencies = new InMemoryAgencyRepo([]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);

        Result<OffPlanProject> result = await useCase.ExecuteAsync(Guid.NewGuid(), ValidCommand());

        Assert.True(result.IsFailure);
        Assert.Equal("market.offplan.not_an_agency", result.Error.Code);
    }

    [Fact]
    public async Task Publish_and_unpublish_work()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);
        var publishUseCase = new PublishOffPlanProjectUseCase(agencies, projects);

        Result<OffPlanProject> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        Assert.True(created.IsSuccess);

        Result<OffPlanProject> published = await publishUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, true);
        Assert.True(published.IsSuccess);
        Assert.True(projects.Items.Single().IsPublished);

        Result<OffPlanProject> unpublished = await publishUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 2, false);
        Assert.True(unpublished.IsSuccess);
        Assert.False(projects.Items.Single().IsPublished);
    }

    [Fact]
    public async Task Delete_fails_when_published()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);
        var publishUseCase = new PublishOffPlanProjectUseCase(agencies, projects);
        var deleteUseCase = new DeleteOffPlanProjectUseCase(agencies, projects);

        Result<OffPlanProject> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        await publishUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, true);

        Result deleteResult = await deleteUseCase.ExecuteAsync(OwnerUserId, created.Value.Id);
        Assert.True(deleteResult.IsFailure);
    }

    [Fact]
    public async Task Delete_succeeds_when_unpublished()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);
        var deleteUseCase = new DeleteOffPlanProjectUseCase(agencies, projects);

        Result<OffPlanProject> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        Result deleteResult = await deleteUseCase.ExecuteAsync(OwnerUserId, created.Value.Id);

        Assert.True(deleteResult.IsSuccess);
        Assert.Empty(projects.Items);
    }

    [Fact]
    public async Task Update_changes_details()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);
        var updateUseCase = new UpdateOffPlanProjectUseCase(agencies, projects);

        Result<OffPlanProject> created = await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        var updateCmd = new SaveOffPlanProjectCommand(
            "برج النور",
            null,
            "Aleppo",
            "الجميلية",
            ConstructionStatus.NearCompletion,
            100,
            null,
            null,
            null);

        Result<OffPlanProject> updated = await updateUseCase.ExecuteAsync(OwnerUserId, created.Value.Id, 1, updateCmd);

        Assert.True(updated.IsSuccess);
        Assert.Equal("برج النور", updated.Value.Name);
        Assert.Equal("Aleppo", updated.Value.Governorate);
        Assert.Equal(100, updated.Value.TotalUnits);
    }

    [Fact]
    public async Task List_mine_returns_only_own_projects()
    {
        var agencies = new InMemoryAgencyRepo([TestAgency]);
        var projects = new InMemoryProjectRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateOffPlanProjectUseCase(agencies, projects, clock);
        var listUseCase = new ListMyOffPlanProjectsUseCase(agencies, projects);

        await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand());
        await createUseCase.ExecuteAsync(OwnerUserId, ValidCommand() with { Name = "برج الأمل" });

        Result<IReadOnlyList<OffPlanProject>> result = await listUseCase.ExecuteAsync(OwnerUserId);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public void Domain_entity_validates_price_range()
    {
        Assert.Throws<ArgumentException>(() =>
            OffPlanProject.Create(
                Guid.NewGuid(), AgencyId, "Test", null, "Damascus", "Center",
                ConstructionStatus.Planned, 10,
                200_000m, 100_000m, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Domain_entity_validates_name_length()
    {
        Assert.Throws<ArgumentException>(() =>
            OffPlanProject.Create(
                Guid.NewGuid(), AgencyId, "ab", null, "Damascus", "Center",
                ConstructionStatus.Planned, 10,
                null, null, null, DateTimeOffset.UtcNow));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class InMemoryAgencyRepo(List<Agency> agencies) : IAgencyRepository
    {
        public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken ct = default) =>
            Task.FromResult(agencies.Find(a => a.Id == agencyId));
        public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(agencies.Find(a => a.OwnerUserId == ownerUserId));
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(agencies.Where(a => ids.Contains(a.OwnerUserId)).ToDictionary(a => a.OwnerUserId));
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(agencies.Where(a => ids.Contains(a.Id)).ToDictionary(a => a.Id));
        public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Agency>>([]);
        public Task<bool> TryAddAsync(Agency agency, CancellationToken ct = default) { agencies.Add(agency); return Task.FromResult(true); }
        public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class InMemoryProjectRepo : IOffPlanProjectRepository
    {
        public List<OffPlanProject> Items { get; } = [];

        public Task<OffPlanProject?> FindByIdAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(p => p.Id == projectId));
        public Task<IReadOnlyList<OffPlanProject>> ListByAgencyAsync(Guid agencyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OffPlanProject>>([.. Items.Where(p => p.AgencyId == agencyId)]);
        public Task<IReadOnlyList<OffPlanProject>> ListPublishedAsync(int offset, int take, string? governorate, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OffPlanProject>>([.. Items.Where(p => p.IsPublished && (governorate == null || p.Governorate == governorate)).Skip(offset).Take(take)]);
        public Task AddAsync(OffPlanProject project, CancellationToken ct = default) { Items.Add(project); return Task.CompletedTask; }
        public Task<bool> TrySaveAsync(OffPlanProject project, long expectedVersion, CancellationToken ct = default)
        {
            int idx = Items.FindIndex(p => p.Id == project.Id);
            if (idx >= 0)
            {
                Items[idx] = OffPlanProject.Restore(
                    project.Id, project.AgencyId, expectedVersion + 1,
                    project.Name, project.Description, project.Governorate, project.District,
                    project.ConstructionStatus, project.TotalUnits, project.MinPriceUsd, project.MaxPriceUsd,
                    project.ExpectedCompletion, project.IsPublished, project.CreatedAt);
            }
            return Task.FromResult(true);
        }
        public Task DeleteAsync(Guid projectId, CancellationToken ct = default)
        {
            Items.RemoveAll(p => p.Id == projectId);
            return Task.CompletedTask;
        }
    }
}
