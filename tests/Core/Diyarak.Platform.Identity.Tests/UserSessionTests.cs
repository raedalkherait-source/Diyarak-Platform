using Xunit;

namespace Diyarak.Platform.Identity.Tests;

public sealed class UserSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_sets_idle_expiry()
    {
        UserSession session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "h1", Now);

        Assert.Equal(Now + UserSession.IdleLifetime, session.ExpiresAt);
        Assert.True(session.IsActive(Now));
    }

    [Fact]
    public void Refresh_with_current_token_rotates_and_slides_expiry()
    {
        UserSession session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "h1", Now);
        DateTimeOffset later = Now.AddDays(10);

        Assert.Equal(UserSessionRefreshOutcome.Refreshed, session.Refresh(true, false, "h2", later));
        Assert.Equal("h2", session.CurrentTokenHash);
        Assert.Equal("h1", session.PreviousTokenHash);
        Assert.Equal(later + UserSession.IdleLifetime, session.ExpiresAt);
    }

    [Fact]
    public void Expiry_never_exceeds_absolute_lifetime()
    {
        UserSession session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "h1", Now);
        DateTimeOffset nearEnd = Now + UserSession.AbsoluteLifetime - TimeSpan.FromDays(1);

        // Keep the session alive by refreshing every 20 days until close to the absolute limit.
        for (DateTimeOffset at = Now.AddDays(20); at < nearEnd; at = at.AddDays(20))
            session.Refresh(true, false, $"h{at.Ticks}", at);
        session.Refresh(true, false, "final", nearEnd);

        Assert.Equal(Now + UserSession.AbsoluteLifetime, session.ExpiresAt);
        Assert.Equal(UserSessionRefreshOutcome.Invalid, session.Refresh(true, false, "x", Now + UserSession.AbsoluteLifetime));
    }

    [Fact]
    public void Previous_token_within_grace_is_stale_and_after_grace_revokes()
    {
        UserSession session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "h1", Now);
        session.Refresh(true, false, "h2", Now);

        Assert.Equal(UserSessionRefreshOutcome.Stale, session.Refresh(false, true, "h3", Now.AddSeconds(10)));
        Assert.True(session.IsActive(Now.AddSeconds(10)));

        Assert.Equal(UserSessionRefreshOutcome.ReuseDetected, session.Refresh(false, true, "h3", Now.AddMinutes(5)));
        Assert.False(session.IsActive(Now.AddMinutes(5)));
        Assert.Equal(UserSessionRefreshOutcome.Invalid, session.Refresh(true, false, "h4", Now.AddMinutes(6)));
    }

    [Fact]
    public void Unknown_token_expired_or_revoked_session_is_invalid()
    {
        UserSession session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), "h1", Now);
        Assert.Equal(UserSessionRefreshOutcome.Invalid, session.Refresh(false, false, "h2", Now));
        Assert.Equal(UserSessionRefreshOutcome.Invalid, session.Refresh(true, false, "h2", Now + UserSession.IdleLifetime));

        session.Revoke(Now);
        Assert.Equal(UserSessionRefreshOutcome.Invalid, session.Refresh(true, false, "h2", Now));
    }

    [Fact]
    public void Start_rejects_empty_identifiers()
    {
        Assert.Throws<ArgumentException>(() => UserSession.Start(Guid.Empty, Guid.NewGuid(), "h", Now));
        Assert.Throws<ArgumentException>(() => UserSession.Start(Guid.NewGuid(), Guid.Empty, "h", Now));
        Assert.Throws<ArgumentException>(() => UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), " ", Now));
    }
}
