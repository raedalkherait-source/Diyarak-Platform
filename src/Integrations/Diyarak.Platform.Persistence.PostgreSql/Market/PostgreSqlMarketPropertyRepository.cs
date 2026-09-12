using Diyarak.Market.Application;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlMarketPropertyRepository
    : IMarketPropertyRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlMarketPropertyRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddAsync(
        MarketProperty property,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(property);

        _context.MarketProperties.Add(
            MarketPropertyRecordMapper.FromDomain(property));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
