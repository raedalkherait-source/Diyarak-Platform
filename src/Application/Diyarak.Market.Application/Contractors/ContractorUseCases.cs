using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Contractors;

public interface IContractorRepository
{
    public Task<Contractor?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<Contractor?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<Contractor>> ListPublishedAsync(int offset, int take, string? governorate, ContractorSpecialty? specialty, CancellationToken cancellationToken = default);
    public Task AddAsync(Contractor contractor, CancellationToken cancellationToken = default);
    public Task<bool> TrySaveAsync(Contractor contractor, long expectedVersion, CancellationToken cancellationToken = default);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public static class ContractorErrors
{
    public static readonly Error InvalidRequest = Error.Validation("market.contractor.invalid_request", "The request is invalid.");
    public static readonly Error NotFound = Error.NotFound("market.contractor.not_found", "Contractor not found.");
    public static readonly Error AlreadyExists = Error.Conflict("market.contractor.already_exists", "You already have a contractor profile.");
    public static readonly Error ConcurrentModification = Error.Conflict("market.contractor.concurrent_modification", "The profile was modified by another request.");
    public static readonly Error CannotDeletePublished = Error.Validation("market.contractor.cannot_delete_published", "Unpublish the profile before deleting.");
}

public sealed record SaveContractorCommand(
    string Name,
    string? Description,
    string Governorate,
    string? District,
    ContractorSpecialty Specialty,
    string Phone);

public sealed class CreateContractorUseCase(
    IContractorRepository repository,
    IClock clock)
{
    public async Task<Result<Contractor>> ExecuteAsync(
        Guid userId,
        SaveContractorCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindByOwnerAsync(userId, cancellationToken);
        if (existing is not null)
            return Result.Failure<Contractor>(ContractorErrors.AlreadyExists);

        Contractor contractor;
        try
        {
            contractor = Contractor.Create(
                Guid.NewGuid(), userId,
                command.Name, command.Description,
                command.Governorate, command.District,
                command.Specialty, command.Phone,
                clock.UtcNow);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Contractor>(ContractorErrors.InvalidRequest);
        }

        await repository.AddAsync(contractor, cancellationToken);
        return Result<Contractor>.Success(contractor);
    }
}

public sealed class UpdateContractorUseCase(IContractorRepository repository)
{
    public async Task<Result<Contractor>> ExecuteAsync(
        Guid userId,
        Guid contractorId,
        long expectedVersion,
        SaveContractorCommand command,
        CancellationToken cancellationToken = default)
    {
        var contractor = await repository.FindByIdAsync(contractorId, cancellationToken);
        if (contractor is null || contractor.OwnerUserId != userId)
            return Result.Failure<Contractor>(ContractorErrors.NotFound);

        try
        {
            contractor.Update(
                command.Name, command.Description,
                command.Governorate, command.District,
                command.Specialty, command.Phone);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Contractor>(ContractorErrors.InvalidRequest);
        }

        if (!await repository.TrySaveAsync(contractor, expectedVersion, cancellationToken))
            return Result.Failure<Contractor>(ContractorErrors.ConcurrentModification);

        return Result<Contractor>.Success(contractor);
    }
}

public sealed class ToggleContractorPublishUseCase(IContractorRepository repository)
{
    public async Task<Result<Contractor>> ExecuteAsync(
        Guid userId,
        Guid contractorId,
        long expectedVersion,
        bool publish,
        CancellationToken cancellationToken = default)
    {
        var contractor = await repository.FindByIdAsync(contractorId, cancellationToken);
        if (contractor is null || contractor.OwnerUserId != userId)
            return Result.Failure<Contractor>(ContractorErrors.NotFound);

        if (publish) contractor.Publish(); else contractor.Unpublish();

        if (!await repository.TrySaveAsync(contractor, expectedVersion, cancellationToken))
            return Result.Failure<Contractor>(ContractorErrors.ConcurrentModification);

        return Result<Contractor>.Success(contractor);
    }
}

public sealed class DeleteContractorUseCase(IContractorRepository repository)
{
    public async Task<Result> ExecuteAsync(
        Guid userId,
        Guid contractorId,
        CancellationToken cancellationToken = default)
    {
        var contractor = await repository.FindByIdAsync(contractorId, cancellationToken);
        if (contractor is null || contractor.OwnerUserId != userId)
            return Result.Failure(ContractorErrors.NotFound);

        if (contractor.IsPublished)
            return Result.Failure(ContractorErrors.CannotDeletePublished);

        await repository.DeleteAsync(contractorId, cancellationToken);
        return Result.Success();
    }
}

public sealed class GetMyContractorUseCase(IContractorRepository repository)
{
    public async Task<Result<Contractor>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var contractor = await repository.FindByOwnerAsync(userId, cancellationToken);
        if (contractor is null)
            return Result.Failure<Contractor>(ContractorErrors.NotFound);

        return Result<Contractor>.Success(contractor);
    }
}

public sealed class ListPublicContractorsUseCase(IContractorRepository repository)
{
    private const int PageSize = 20;

    public async Task<Result<IReadOnlyList<Contractor>>> ExecuteAsync(
        int page,
        string? governorate,
        ContractorSpecialty? specialty,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        int offset = (page - 1) * PageSize;
        var items = await repository.ListPublishedAsync(offset, PageSize + 1, governorate, specialty, cancellationToken);
        return Result<IReadOnlyList<Contractor>>.Success(items);
    }
}
