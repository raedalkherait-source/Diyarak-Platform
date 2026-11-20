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

    public Task SendAsync(PhoneNumber phoneNumber, string code, CancellationToken cancellationToken = default)
    {
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
      IIdentityUnitOfWork
{
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
            _stagedChallenges.Clear();
            _stagedAccounts.Clear();
            return Task.FromResult(false);
        }

        foreach (PhoneVerificationChallenge challenge in _stagedChallenges)
            Challenges[challenge.Id] = challenge;

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
