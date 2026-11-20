using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlDraftListingDeletionStore
    : IDraftListingDeletionStore
{
    private readonly PlatformDbContext _context;

    public PostgreSqlDraftListingDeletionStore(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<bool> TryDeleteListingAsync(
        Guid listingId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        int deleted = await _context.MarketListings
            .Where(listing =>
                listing.Id == listingId &&
                listing.Version == expectedVersion &&
                listing.Status == (int)ListingStatus.Draft)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 1;
    }

    public async Task<IReadOnlyList<string>> DeletePhotosAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        List<string> storageKeys = await _context.MarketListingPhotos
            .Where(photo => photo.ListingId == listingId)
            .Select(photo => photo.StorageKey)
            .ToListAsync(cancellationToken);

        await _context.MarketListingPhotos
            .Where(photo => photo.ListingId == listingId)
            .ExecuteDeleteAsync(cancellationToken);

        return storageKeys;
    }

    public async Task DeletePropertyIfUnreferencedAsync(
        Guid propertyId,
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        await _context.MarketProperties
            .Where(property =>
                property.Id == propertyId &&
                property.OwnerUserId == ownerUserId &&
                !_context.MarketListings.Any(listing =>
                    listing.SubjectId == propertyId &&
                    listing.SubjectType == MarketListingSubjectTypes.Property))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
