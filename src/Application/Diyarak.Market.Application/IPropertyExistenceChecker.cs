namespace Diyarak.Market.Application;

public interface IPropertyExistenceChecker
{
    public Task<bool> ExistsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);
}
