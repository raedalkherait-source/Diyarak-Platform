using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Platform.Identity.Application.Tests;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
}

internal sealed class CapturingOtpSender : IOtpSender
{
    public List<(PhoneNumber Phone, string Code)> Sent { get; } = [];

    public bool Fail { get; set; }

    public Task SendAsync(PhoneNumber phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        if (Fail)
            throw new OtpDeliveryException("gateway down");

        Sent.Add((phoneNumber, code));
        return Task.CompletedTask;
    }
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public string Issuer => "https://auth.diyarak.test";

    public IssuedAccessToken Issue(Guid userId) =>
        new($"token-for-{userId:N}", new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
}

/// <summary>
/// Stages changes like the PostgreSQL adapter and only makes them visible after SaveChangesAsync.
/// </summary>
internal sealed class InMemoryIdentityStore
    : IPhoneVerificationChallengeRepository,
      IPhoneAccountRepository,
      IIdentityUnitOfWork,
      IUserSessionRepository
{
    private readonly List<UserSession> _stagedSessions = [];

    public Dictionary<Guid, UserSession> Sessions { get; } = [];

    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _stagedSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Sessions.TryGetValue(sessionId, out UserSession? session)
                ? UserSession.Restore(session.Id, session.UserId, session.CurrentTokenHash, session.PreviousTokenHash, session.CreatedAt, session.LastRotatedAt, session.ExpiresAt, session.RevokedAt)
                : null);

    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _stagedSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        foreach (UserSession session in Sessions.Values.Where(session => session.UserId == userId))
            session.Revoke(now);
        return Task.CompletedTask;
    }

    private readonly List<PhoneVerificationChallenge> _stagedChallenges = [];
    private readonly List<(PhoneAccount Account, ExternalIdentity Identity)> _stagedAccounts = [];

    public Dictionary<Guid, PhoneVerificationChallenge> Challenges { get; } = [];

    public Dictionary<string, PhoneAccount> Accounts { get; } = [];

    public Dictionary<ExternalIdentity, Guid> Mappings { get; } = [];

    public int SaveCount { get; private set; }

    public bool FailNextSave { get; set; }

    public Task AddAsync(PhoneVerificationChallenge challenge, CancellationToken cancellationToken = default)
    {
        _stagedChallenges.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<PhoneVerificationChallenge?> GetLatestAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Challenges.Values
                .Where(challenge => challenge.PhoneNumber == phoneNumber)
                .OrderByDescending(challenge => challenge.IssuedAt)
                .FirstOrDefault());

    public Task<int> CountIssuedSinceAsync(PhoneNumber phoneNumber, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Challenges.Values.Count(
                challenge => challenge.PhoneNumber == phoneNumber && challenge.IssuedAt >= since));

    public Task UpdateAsync(PhoneVerificationChallenge challenge, CancellationToken cancellationToken = default)
    {
        _stagedChallenges.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<PhoneAccount?> FindAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(Accounts.GetValueOrDefault(phoneNumber.Value));

    public Task AddAsync(PhoneAccount account, ExternalIdentity tokenIdentity, CancellationToken cancellationToken = default)
    {
        _stagedAccounts.Add((account, tokenIdentity));
        return Task.CompletedTask;
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        if (FailNextSave)
        {
            FailNextSave = false;
            _stagedSessions.Clear();
            _stagedChallenges.Clear();
            _stagedAccounts.Clear();
            return Task.FromResult(false);
        }

        foreach (PhoneVerificationChallenge challenge in _stagedChallenges)
            Challenges[challenge.Id] = challenge;

        foreach (UserSession session in _stagedSessions)
            Sessions[session.Id] = session;
        _stagedSessions.Clear();

        foreach ((PhoneAccount account, ExternalIdentity identity) in _stagedAccounts)
        {
            Accounts[account.PhoneNumber.Value] = account;
            Mappings[identity] = account.UserId;
        }

        _stagedChallenges.Clear();
        _stagedAccounts.Clear();
        return Task.FromResult(true);
    }
}
