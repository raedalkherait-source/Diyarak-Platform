using Diyarak.Market.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlPropertyExistenceChecker
    : IPropertyExistenceChecker
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPropertyExistenceChecker(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public Task<bool> ExistsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return _context.MarketProperties
            .AsNoTracking()
            .AnyAsync(
                record => record.Id == propertyId,
                cancellationToken);
    }
}
