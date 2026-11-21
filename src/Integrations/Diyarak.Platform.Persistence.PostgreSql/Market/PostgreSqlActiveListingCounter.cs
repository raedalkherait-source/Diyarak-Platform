using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlActiveListingCounter : IActiveListingCounter
{
    private static readonly int[] ActiveStatuses = [(int)ListingStatus.Published, (int)ListingStatus.Paused];

    private readonly PlatformDbContext _context;

    public PostgreSqlActiveListingCounter(PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<int> CountActiveByPublisherAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
        _context.MarketListings.AsNoTracking()
            .CountAsync(
                listing => listing.PublisherUserId == publisherUserId && ActiveStatuses.Contains(listing.Status),
                cancellationToken);
}
