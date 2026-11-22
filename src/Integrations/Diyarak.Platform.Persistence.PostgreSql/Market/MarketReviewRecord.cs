namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketReviewRecord
{
    public Guid Id { get; set; }

    public Guid AuthorUserId { get; set; }

    public Guid SubjectId { get; set; }

    public int SubjectType { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
