namespace Diyarak.Market.Application;

public interface IPropertyListingAuthorizationChecker
{
    public Task<bool> CanCreateListingAsync(
        Guid propertyId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
