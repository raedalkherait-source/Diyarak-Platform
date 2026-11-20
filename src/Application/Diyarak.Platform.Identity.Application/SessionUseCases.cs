using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Platform.Identity.Application;

public interface IUserSessionRepository
{
    /// <summary>Stages a new session; committed by <see cref="IIdentityUnitOfWork"/>.</summary>
    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default);

    public Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages changes to a session loaded through <see cref="FindAsync"/>. The commit fails when
    /// another request rotated the same session concurrently.
    /// </summary>
    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active session of the user immediately.</summary>
    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);

public sealed record RefreshedSession(IssuedAccessToken AccessToken, IssuedRefreshToken RefreshToken);

public static class SessionErrors
{
    public static Error Invalid { get; } =
        Error.Validation("identity.session.invalid", "The session has expired or was signed out. Sign in again.");

    public static Error Stale { get; } =
        Error.Conflict("identity.session.stale", "The session was just renewed by another request. Use the latest token.");
}

internal static class UserSessions
{
    internal static async Task<IssuedRefreshToken> StartAsync(
        IUserSessionRepository sessions,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Guid sessionId = Guid.NewGuid();
        (string token, string hash) = RefreshTokens.Create(sessionId);
        var session = UserSession.Start(sessionId, userId, hash, now);
        await sessions.AddAsync(session, cancellationToken);
        return new IssuedRefreshToken(token, session.ExpiresAt);
    }
}

/// <summary>Exchanges a valid refresh token for a new access token and a rotated refresh token.</summary>
public sealed class RefreshSessionUseCase(
    IUserSessionRepository sessions,
    IIdentityUnitOfWork unitOfWork,
    IAccessTokenIssuer tokenIssuer,
    IClock clock)
{
    public async Task<Result<RefreshedSession>> ExecuteAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!RefreshTokens.TryParse(refreshToken, out Guid sessionId, out string secret))
            return Result.Failure<RefreshedSession>(SessionErrors.Invalid);

        UserSession? session = await sessions.FindAsync(sessionId, cancellationToken);
        if (session is null)
            return Result.Failure<RefreshedSession>(SessionErrors.Invalid);

        string presentedHash = RefreshTokens.Hash(secret);
        (string newToken, string newHash) = RefreshTokens.Create(sessionId);
        DateTimeOffset now = clock.UtcNow;

        UserSessionRefreshOutcome outcome = session.Refresh(
            RefreshTokens.HashEquals(session.CurrentTokenHash, presentedHash),
            RefreshTokens.HashEquals(session.PreviousTokenHash, presentedHash),
            newHash,
            now);

        switch (outcome)
        {
            case UserSessionRefreshOutcome.Stale:
                return Result.Failure<RefreshedSession>(SessionErrors.Stale);

            case UserSessionRefreshOutcome.ReuseDetected:
                await sessions.UpdateAsync(session, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<RefreshedSession>(SessionErrors.Invalid);

            case UserSessionRefreshOutcome.Invalid:
                return Result.Failure<RefreshedSession>(SessionErrors.Invalid);
        }

        await sessions.UpdateAsync(session, cancellationToken);

        // A concurrent rotation of the same token won; this request lost the race.
        if (!await unitOfWork.SaveChangesAsync(cancellationToken))
            return Result.Failure<RefreshedSession>(SessionErrors.Stale);

        return Result.Success(
            new RefreshedSession(
                tokenIssuer.Issue(session.UserId),
                new IssuedRefreshToken(newToken, session.ExpiresAt)));
    }
}

/// <summary>Signs out one device. Always succeeds so callers learn nothing about the token.</summary>
public sealed class RevokeSessionUseCase(
    IUserSessionRepository sessions,
    IIdentityUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task ExecuteAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!RefreshTokens.TryParse(refreshToken, out Guid sessionId, out string secret))
            return;

        UserSession? session = await sessions.FindAsync(sessionId, cancellationToken);
        if (session is null)
            return;

        string hash = RefreshTokens.Hash(secret);
        if (!RefreshTokens.HashEquals(session.CurrentTokenHash, hash) && !RefreshTokens.HashEquals(session.PreviousTokenHash, hash))
            return;

        session.Revoke(clock.UtcNow);
        await sessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Signs a user out of every device.</summary>
public sealed class RevokeAllSessionsUseCase(IUserSessionRepository sessions, IClock clock)
{
    public Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));

        return sessions.RevokeAllAsync(userId, clock.UtcNow, cancellationToken);
    }
}
