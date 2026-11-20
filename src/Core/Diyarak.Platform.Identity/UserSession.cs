using Diyarak.Platform.SharedKernel;

namespace Diyarak.Platform.Identity;

public enum UserSessionRefreshOutcome
{
    Refreshed = 1,

    /// <summary>The token is unknown, expired, or the session was revoked.</summary>
    Invalid = 2,

    /// <summary>The previous token was presented shortly after rotation, typically by a parallel request.</summary>
    Stale = 3,

    /// <summary>A rotated token was replayed after the grace period; the session has been revoked.</summary>
    ReuseDetected = 4,
}

/// <summary>
/// A long-lived sign-in on one device. Only hashes of refresh tokens are stored. Each refresh
/// rotates the token; replaying an old token after a short grace period revokes the session,
/// which limits the damage of a stolen refresh token.
/// </summary>
public sealed class UserSession : AggregateRoot<Guid>
{
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromDays(30);

    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(90);

    public static readonly TimeSpan RotationGracePeriod = TimeSpan.FromSeconds(30);

    private UserSession(
        Guid id,
        Guid userId,
        string currentTokenHash,
        string? previousTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset lastRotatedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt)
        : base(id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Session identifier cannot be empty.", nameof(id));

        if (userId == Guid.Empty)
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));

        ArgumentException.ThrowIfNullOrWhiteSpace(currentTokenHash);

        UserId = userId;
        CurrentTokenHash = currentTokenHash;
        PreviousTokenHash = previousTokenHash;
        CreatedAt = createdAt;
        LastRotatedAt = lastRotatedAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
    }

    public Guid UserId { get; }

    public string CurrentTokenHash { get; private set; }

    public string? PreviousTokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastRotatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static UserSession Start(Guid id, Guid userId, string tokenHash, DateTimeOffset now) =>
        new(id, userId, tokenHash, null, now, now, now + IdleLifetime, null);

    public static UserSession Restore(
        Guid id,
        Guid userId,
        string currentTokenHash,
        string? previousTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset lastRotatedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt) =>
        new(id, userId, currentTokenHash, previousTokenHash, createdAt, lastRotatedAt, expiresAt, revokedAt);

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>
    /// Rotates the session when the presented token matches the current token. The caller compares
    /// token hashes in constant time and passes the results in <paramref name="matchesCurrent"/>
    /// and <paramref name="matchesPrevious"/>.
    /// </summary>
    public UserSessionRefreshOutcome Refresh(
        bool matchesCurrent,
        bool matchesPrevious,
        string newTokenHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTokenHash);

        if (!IsActive(now))
            return UserSessionRefreshOutcome.Invalid;

        if (matchesCurrent)
        {
            PreviousTokenHash = CurrentTokenHash;
            CurrentTokenHash = newTokenHash;
            LastRotatedAt = now;

            DateTimeOffset idleExpiry = now + IdleLifetime;
            DateTimeOffset absoluteExpiry = CreatedAt + AbsoluteLifetime;
            ExpiresAt = idleExpiry < absoluteExpiry ? idleExpiry : absoluteExpiry;
            return UserSessionRefreshOutcome.Refreshed;
        }

        if (matchesPrevious)
        {
            if (now - LastRotatedAt <= RotationGracePeriod)
                return UserSessionRefreshOutcome.Stale;

            Revoke(now);
            return UserSessionRefreshOutcome.ReuseDetected;
        }

        return UserSessionRefreshOutcome.Invalid;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
