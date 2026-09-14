using Diyarak.Market.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlPropertyListingAuthorizationChecker
    : IPropertyListingAuthorizationChecker
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPropertyListingAuthorizationChecker(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<bool> CanCreateListingAsync(
        Guid propertyId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (propertyId == Guid.Empty || actorUserId == Guid.Empty)
            return Task.FromResult(false);

        return _context.MarketProperties
            .AsNoTracking()
            .AnyAsync(
                property =>
                    property.Id == propertyId &&
                    property.OwnerUserId == actorUserId,
                cancellationToken);
    }
}
