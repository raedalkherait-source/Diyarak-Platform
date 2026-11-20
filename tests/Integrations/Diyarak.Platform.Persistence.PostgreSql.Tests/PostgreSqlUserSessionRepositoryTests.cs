using Diyarak.Platform.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlUserSessionRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Session_round_trips_and_rotation_persists()
    {
        string database = Guid.NewGuid().ToString();
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "hash-1", Now);

        await using (PlatformDbContext context = Create(database))
        {
            await new PostgreSqlUserSessionRepository(context).AddAsync(session);
            Assert.True(await new PostgreSqlIdentityUnitOfWork(context).SaveChangesAsync());
        }

        await using (PlatformDbContext context = Create(database))
        {
            var repository = new PostgreSqlUserSessionRepository(context);
            UserSession loaded = (await repository.FindAsync(session.Id))!;
            Assert.Equal(session.UserId, loaded.UserId);
            Assert.Equal(session.ExpiresAt, loaded.ExpiresAt);

            loaded.Refresh(true, false, "hash-2", Now.AddDays(1));
            await repository.UpdateAsync(loaded);
            Assert.True(await new PostgreSqlIdentityUnitOfWork(context).SaveChangesAsync());
        }

        await using (PlatformDbContext context = Create(database))
        {
            UserSession reloaded = (await new PostgreSqlUserSessionRepository(context).FindAsync(session.Id))!;
            Assert.Equal("hash-2", reloaded.CurrentTokenHash);
            Assert.Equal("hash-1", reloaded.PreviousTokenHash);
            Assert.Equal(Now.AddDays(1), reloaded.LastRotatedAt);
        }
    }

    [Fact]
    public async Task Concurrent_rotation_of_the_same_token_lets_only_one_win()
    {
        string database = Guid.NewGuid().ToString();
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "hash-1", Now);

        await using (PlatformDbContext context = Create(database))
        {
            await new PostgreSqlUserSessionRepository(context).AddAsync(session);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext first = Create(database);
        await using PlatformDbContext second = Create(database);
        var firstRepository = new PostgreSqlUserSessionRepository(first);
        var secondRepository = new PostgreSqlUserSessionRepository(second);

        UserSession a = (await firstRepository.FindAsync(session.Id))!;
        UserSession b = (await secondRepository.FindAsync(session.Id))!;
        a.Refresh(true, false, "hash-a", Now);
        b.Refresh(true, false, "hash-b", Now);

        await firstRepository.UpdateAsync(a);
        Assert.True(await new PostgreSqlIdentityUnitOfWork(first).SaveChangesAsync());

        await secondRepository.UpdateAsync(b);
        Assert.False(await new PostgreSqlIdentityUnitOfWork(second).SaveChangesAsync());
    }

    [Fact]
    public async Task Revoke_all_revokes_only_the_users_active_sessions()
    {
        string database = Guid.NewGuid().ToString();
        Guid user = Guid.NewGuid();
        Guid other = Guid.NewGuid();

        await using (PlatformDbContext context = Create(database))
        {
            var repository = new PostgreSqlUserSessionRepository(context);
            await repository.AddAsync(UserSession.Start(Guid.NewGuid(), user, "a", Now));
            await repository.AddAsync(UserSession.Start(Guid.NewGuid(), user, "b", Now));
            await repository.AddAsync(UserSession.Start(Guid.NewGuid(), other, "c", Now));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = Create(database))
            await new PostgreSqlUserSessionRepository(context).RevokeAllAsync(user, Now.AddHours(1));

        await using (PlatformDbContext context = Create(database))
        {
            List<UserSessionRecord> sessions = await context.UserSessions.ToListAsync();
            Assert.All(sessions.Where(session => session.UserId == user), session => Assert.Equal(Now.AddHours(1), session.RevokedAt));
            Assert.Null(sessions.Single(session => session.UserId == other).RevokedAt);
        }
    }

    private static PlatformDbContext Create(string database) =>
        new(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(database).Options);
}
