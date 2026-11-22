using Diyarak.Market.Application.Reviews;
using Diyarak.Market.Company;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlReviewRepository(PlatformDbContext db) : IReviewRepository
{
    public async Task<Review?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.MarketReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<Review?> FindByAuthorAndSubjectAsync(
        Guid authorUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        var record = await db.MarketReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AuthorUserId == authorUserId && r.SubjectId == subjectId, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<Review>> ListBySubjectAsync(
        Guid subjectId, int offset, int take, CancellationToken cancellationToken)
    {
        var records = await db.MarketReviews
            .AsNoTracking()
            .Where(r => r.SubjectId == subjectId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(offset)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task<(double Average, int Count)?> GetSubjectAverageAsync(
        Guid subjectId, CancellationToken cancellationToken)
    {
        var stats = await db.MarketReviews
            .AsNoTracking()
            .Where(r => r.SubjectId == subjectId)
            .GroupBy(_ => 1)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats is null || stats.Count == 0)
            return null;

        return (stats.Average, stats.Count);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        db.MarketReviews.Add(FromDomain(review));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.MarketReviews.FindAsync([id], cancellationToken);
        if (record is not null)
        {
            db.MarketReviews.Remove(record);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static MarketReviewRecord FromDomain(Review review) => new()
    {
        Id = review.Id,
        AuthorUserId = review.AuthorUserId,
        SubjectId = review.SubjectId,
        SubjectType = (int)review.SubjectType,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt,
    };

    private static Review ToDomain(MarketReviewRecord record) =>
        Review.Restore(record.Id, record.AuthorUserId, record.SubjectId,
            (ReviewSubjectType)record.SubjectType, record.Rating, record.Comment, record.CreatedAt);
}
