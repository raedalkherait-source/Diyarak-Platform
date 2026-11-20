namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class UserSessionRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CurrentTokenHash { get; set; } = string.Empty;

    public string? PreviousTokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastRotatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
