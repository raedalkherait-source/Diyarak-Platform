using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlUserSessionRepository : IUserSessionRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlUserSessionRepository(PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        _context.UserSessions.Add(ToRecord(session));
        return Task.CompletedTask;
    }

    public async Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // Tracked so UpdateAsync compares the concurrency token with the value observed here.
        UserSessionRecord? record = await _context.UserSessions
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        return record is null
            ? null
            : UserSession.Restore(
                record.Id,
                record.UserId,
                record.CurrentTokenHash,
                record.PreviousTokenHash,
                record.CreatedAt,
                record.LastRotatedAt,
                record.ExpiresAt,
                record.RevokedAt);
    }

    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        UserSessionRecord record =
            _context.UserSessions.Local.SingleOrDefault(candidate => candidate.Id == session.Id)
            ?? throw new InvalidOperationException("The session must be loaded through this repository before it is updated.");

        _context.Entry(record).CurrentValues.SetValues(ToRecord(session));
        return Task.CompletedTask;
    }

    public async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        List<UserSessionRecord> active = await _context.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (UserSessionRecord session in active)
            session.RevokedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserSessionRecord ToRecord(UserSession session) =>
        new()
        {
            Id = session.Id,
            UserId = session.UserId,
            CurrentTokenHash = session.CurrentTokenHash,
            PreviousTokenHash = session.PreviousTokenHash,
            CreatedAt = session.CreatedAt,
            LastRotatedAt = session.LastRotatedAt,
            ExpiresAt = session.ExpiresAt,
            RevokedAt = session.RevokedAt,
        };
}
