using Diyarak.Market.Application;
using Diyarak.Market.Application.Favorites;
using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlListingFavoriteStore
    : IListingFavoriteStore
{
    private const string UniqueViolationSqlState = "23505";

    private readonly PlatformDbContext _context;

    public PostgreSqlListingFavoriteStore(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<bool> AddAsync(
        Guid userId,
        Guid listingId,
        DateTimeOffset savedAt,
        CancellationToken cancellationToken = default)
    {
        var record = new MarketListingFavoriteRecord
        {
            UserId = userId,
            ListingId = listingId,
            CreatedAt = savedAt,
        };

        _context.MarketListingFavorites.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            // A parallel request saved the same Listing first.
            return false;
        }
        finally
        {
            _context.Entry(record).State = EntityState.Detached;
        }
    }

    public Task RemoveAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return _context.MarketListingFavorites
            .Where(favorite => favorite.UserId == userId && favorite.ListingId == listingId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.MarketListingFavorites
            .CountAsync(favorite => favorite.UserId == userId, cancellationToken);
    }

    public Task<bool> ContainsAsync(
        Guid userId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return _context.MarketListingFavorites
            .AnyAsync(favorite => favorite.UserId == userId && favorite.ListingId == listingId, cancellationToken);
    }

    public Task<int> CountForListingAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return _context.MarketListingFavorites
            .CountAsync(favorite => favorite.ListingId == listingId, cancellationToken);
    }

    public async Task<IReadOnlyList<PublishedListingProjection>> ListPublishedAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        var records = await _context.MarketListingFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Join(
                _context.MarketListings.AsNoTracking().Where(listing =>
                    listing.Status == (int)ListingStatus.Published &&
                    listing.SubjectType == MarketListingSubjectTypes.Property),
                favorite => favorite.ListingId,
                listing => listing.Id,
                (favorite, listing) => new { favorite.CreatedAt, Listing = listing })
            .Join(
                _context.MarketProperties.AsNoTracking(),
                pair => pair.Listing.SubjectId,
                property => property.Id,
                (pair, property) => new { pair.CreatedAt, pair.Listing, Property = property })
            .OrderByDescending(row => row.CreatedAt)
            .ThenBy(row => row.Listing.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. records.Select(row => new PublishedListingProjection(
            MarketListingRecordMapper.ToDomain(row.Listing),
            MarketPropertyRecordMapper.ToDomain(row.Property)))];
    }
}
