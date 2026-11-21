using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.OffPlanProjects;

public interface IOffPlanProjectRepository
{
    public Task<OffPlanProject?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<OffPlanProject>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<OffPlanProject>> ListPublishedAsync(int offset, int take, string? governorate, CancellationToken cancellationToken = default);

    public Task AddAsync(OffPlanProject project, CancellationToken cancellationToken = default);

    public Task<bool> TrySaveAsync(OffPlanProject project, long expectedVersion, CancellationToken cancellationToken = default);

    public Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public static class OffPlanProjectErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.offplan.invalid_request", "The project details are invalid.");

    public static Error NotFound { get; } =
        Error.NotFound("market.offplan.not_found", "The off-plan project does not exist.");

    public static Error NotAnAgency { get; } =
        Error.Validation("market.offplan.not_an_agency", "Only agencies can manage off-plan projects.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict("market.offplan.concurrent_modification", "The project changed meanwhile; reload and try again.");

    public static Error InvalidTransition { get; } =
        Error.Conflict("market.offplan.invalid_transition", "This action is not possible in the project's current state.");
}

public sealed record SaveOffPlanProjectCommand(
    string Name,
    string? Description,
    string Governorate,
    string District,
    ConstructionStatus ConstructionStatus,
    int TotalUnits,
    decimal? MinPriceUsd,
    decimal? MaxPriceUsd,
    DateOnly? ExpectedCompletion);

public sealed class CreateOffPlanProjectUseCase(IAgencyRepository agencies, IOffPlanProjectRepository projects, IClock clock)
{
    public async Task<Result<OffPlanProject>> ExecuteAsync(Guid actorUserId, SaveOffPlanProjectCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (actorUserId == Guid.Empty)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.NotAnAgency);

        try
        {
            OffPlanProject project = OffPlanProject.Create(
                Guid.NewGuid(),
                agency.Id,
                command.Name,
                command.Description,
                command.Governorate,
                command.District,
                command.ConstructionStatus,
                command.TotalUnits,
                command.MinPriceUsd,
                command.MaxPriceUsd,
                command.ExpectedCompletion,
                clock.UtcNow);

            await projects.AddAsync(project, cancellationToken);
            return Result.Success(project);
        }
        catch (ArgumentException)
        {
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidRequest);
        }
    }
}

public sealed class UpdateOffPlanProjectUseCase(IAgencyRepository agencies, IOffPlanProjectRepository projects)
{
    public async Task<Result<OffPlanProject>> ExecuteAsync(Guid actorUserId, Guid projectId, long expectedVersion, SaveOffPlanProjectCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (actorUserId == Guid.Empty)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.NotAnAgency);

        OffPlanProject? project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null || project.AgencyId != agency.Id)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.NotFound);

        if (project.Version != expectedVersion)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.ConcurrentModification);

        try
        {
            project.Update(
                command.Name,
                command.Description,
                command.Governorate,
                command.District,
                command.ConstructionStatus,
                command.TotalUnits,
                command.MinPriceUsd,
                command.MaxPriceUsd,
                command.ExpectedCompletion);
        }
        catch (ArgumentException)
        {
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidRequest);
        }

        return await projects.TrySaveAsync(project, expectedVersion, cancellationToken)
            ? Result.Success(project)
            : Result.Failure<OffPlanProject>(OffPlanProjectErrors.ConcurrentModification);
    }
}

public sealed class PublishOffPlanProjectUseCase(IAgencyRepository agencies, IOffPlanProjectRepository projects)
{
    public async Task<Result<OffPlanProject>> ExecuteAsync(Guid actorUserId, Guid projectId, long expectedVersion, bool publish, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.NotAnAgency);

        OffPlanProject? project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null || project.AgencyId != agency.Id)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.NotFound);

        if (project.Version != expectedVersion)
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.ConcurrentModification);

        try
        {
            if (publish)
                project.Publish();
            else
                project.Unpublish();
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OffPlanProject>(OffPlanProjectErrors.InvalidTransition);
        }

        return await projects.TrySaveAsync(project, expectedVersion, cancellationToken)
            ? Result.Success(project)
            : Result.Failure<OffPlanProject>(OffPlanProjectErrors.ConcurrentModification);
    }
}

public sealed class DeleteOffPlanProjectUseCase(IAgencyRepository agencies, IOffPlanProjectRepository projects)
{
    public async Task<Result> ExecuteAsync(Guid actorUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure(OffPlanProjectErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure(OffPlanProjectErrors.NotAnAgency);

        OffPlanProject? project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null || project.AgencyId != agency.Id)
            return Result.Failure(OffPlanProjectErrors.NotFound);

        if (project.IsPublished)
            return Result.Failure(OffPlanProjectErrors.InvalidTransition);

        await projects.DeleteAsync(projectId, cancellationToken);
        return Result.Success();
    }
}

public sealed class ListMyOffPlanProjectsUseCase(IAgencyRepository agencies, IOffPlanProjectRepository projects)
{
    public async Task<Result<IReadOnlyList<OffPlanProject>>> ExecuteAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure<IReadOnlyList<OffPlanProject>>(OffPlanProjectErrors.InvalidRequest);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<IReadOnlyList<OffPlanProject>>(OffPlanProjectErrors.NotAnAgency);

        IReadOnlyList<OffPlanProject> list = await projects.ListByAgencyAsync(agency.Id, cancellationToken);
        return Result.Success(list);
    }
}

public sealed record PublicOffPlanProjectPage(IReadOnlyList<PublicOffPlanProjectItem> Items, bool HasMore);

public sealed record PublicOffPlanProjectItem(OffPlanProject Project, Agency Agency);

public sealed class ListPublicOffPlanProjectsUseCase(IOffPlanProjectRepository projects, IAgencyRepository agencies)
{
    public const int PageSize = 24;

    public async Task<Result<PublicOffPlanProjectPage>> ExecuteAsync(int page, string? governorate, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100)
            return Result.Failure<PublicOffPlanProjectPage>(OffPlanProjectErrors.InvalidRequest);

        IReadOnlyList<OffPlanProject> found = await projects.ListPublishedAsync(
            (page - 1) * PageSize,
            PageSize + 1,
            governorate,
            cancellationToken);

        OffPlanProject[] items = [.. found.Take(PageSize)];

        IReadOnlyDictionary<Guid, Agency> agencyMap = items.Length > 0
            ? await agencies.FindByIdsAsync([.. items.Select(p => p.AgencyId).Distinct()], cancellationToken)
            : new Dictionary<Guid, Agency>();

        PublicOffPlanProjectItem[] result = [.. items
            .Where(p => agencyMap.ContainsKey(p.AgencyId))
            .Select(p => new PublicOffPlanProjectItem(p, agencyMap[p.AgencyId]))];

        return Result.Success(new PublicOffPlanProjectPage(result, found.Count > PageSize));
    }
}

public sealed class GetPublicOffPlanProjectUseCase(IOffPlanProjectRepository projects, IAgencyRepository agencies)
{
    public async Task<Result<PublicOffPlanProjectItem>> ExecuteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
            return Result.Failure<PublicOffPlanProjectItem>(OffPlanProjectErrors.InvalidRequest);

        OffPlanProject? project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null || !project.IsPublished)
            return Result.Failure<PublicOffPlanProjectItem>(OffPlanProjectErrors.NotFound);

        Agency? agency = await agencies.FindByIdAsync(project.AgencyId, cancellationToken);
        if (agency is null)
            return Result.Failure<PublicOffPlanProjectItem>(OffPlanProjectErrors.NotFound);

        return Result.Success(new PublicOffPlanProjectItem(project, agency));
    }
}
