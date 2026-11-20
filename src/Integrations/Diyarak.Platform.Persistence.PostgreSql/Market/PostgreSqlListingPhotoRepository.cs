using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlListingPhotoRepository
    : IListingPhotoRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly PlatformDbContext _context;

    public PostgreSqlListingPhotoRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<IReadOnlyList<ListingPhoto>> ListByListingAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        List<MarketListingPhotoRecord> records =
            await _context.MarketListingPhotos
                .AsNoTracking()
                .Where(photo => photo.ListingId == listingId)
                .OrderBy(photo => photo.Position)
                .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>>> ListByListingsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listingIds);

        if (listingIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ListingPhoto>>();

        Guid[] ids = [.. listingIds.Distinct()];

        List<MarketListingPhotoRecord> records =
            await _context.MarketListingPhotos
                .AsNoTracking()
                .Where(photo => ids.Contains(photo.ListingId))
                .OrderBy(photo => photo.ListingId)
                .ThenBy(photo => photo.Position)
                .ToListAsync(cancellationToken);

        return records
            .GroupBy(record => record.ListingId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ListingPhoto>)[.. group.Select(ToDomain)]);
    }

    public async Task<ListingPhoto?> FindAsync(
        Guid listingId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        MarketListingPhotoRecord? record =
            await _context.MarketListingPhotos
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    photo => photo.Id == photoId && photo.ListingId == listingId,
                    cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<bool> TryAddAsync(
        ListingPhoto photo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photo);

        var record = new MarketListingPhotoRecord
        {
            Id = photo.Id,
            ListingId = photo.ListingId,
            StorageKey = photo.StorageKey,
            Format = (int)photo.Format,
            SizeBytes = photo.SizeBytes,
            Position = photo.Position,
            UploadedAt = photo.UploadedAt,
        };

        _context.MarketListingPhotos.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException { SqlState: UniqueViolationSqlState })
        {
            _context.Entry(record).State = EntityState.Detached;
            return false;
        }
    }

    public async Task DeleteAsync(
        ListingPhoto photo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photo);

        MarketListingPhotoRecord? record =
            await _context.MarketListingPhotos
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == photo.Id && candidate.ListingId == photo.ListingId,
                    cancellationToken);

        if (record is null)
            return;

        _context.MarketListingPhotos.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ListingPhoto ToDomain(MarketListingPhotoRecord record) =>
        new(
            record.Id,
            record.ListingId,
            record.StorageKey,
            (ListingPhotoFormat)record.Format,
            record.SizeBytes,
            record.Position,
            record.UploadedAt);
}
