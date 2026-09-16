using Diyarak.Platform.BuildingBlocks;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed class ListOwnedPropertiesUseCase
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IMarketPropertyRepository _propertyRepository;

    public ListOwnedPropertiesUseCase(
        IMarketPropertyRepository propertyRepository)
    {
        ArgumentNullException.ThrowIfNull(propertyRepository);
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<OwnedPropertyPage>> ExecuteAsync(
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<OwnedPropertyPage>(
                ListOwnedPropertiesErrors.InvalidActorIdentifier);
        }

        if (page <= 0 ||
            pageSize <= 0 ||
            pageSize > MaximumPageSize)
        {
            return Result.Failure<OwnedPropertyPage>(
                ListOwnedPropertiesErrors.InvalidPagination);
        }

        long skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return Result.Failure<OwnedPropertyPage>(
                ListOwnedPropertiesErrors.InvalidPagination);
        }

        IReadOnlyList<MarketProperty> candidates =
            await _propertyRepository.FindPageByOwnerUserIdAsync(
                actorUserId,
                (int)skip,
                pageSize + 1,
                cancellationToken);

        if (candidates.Any(
                property =>
                    property.OwnerUserId != actorUserId))
        {
            throw new InvalidOperationException(
                "The owned Property query returned a Property for another owner or an unowned legacy Property.");
        }

        bool hasMore = candidates.Count > pageSize;
        MarketProperty[] items = candidates
            .Take(pageSize)
            .ToArray();

        return Result.Success(
            new OwnedPropertyPage(
                items,
                page,
                pageSize,
                hasMore));
    }
}
