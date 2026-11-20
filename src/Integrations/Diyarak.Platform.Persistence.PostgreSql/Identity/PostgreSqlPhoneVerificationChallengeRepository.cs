using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlPhoneVerificationChallengeRepository
    : IPhoneVerificationChallengeRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPhoneVerificationChallengeRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task AddAsync(
        PhoneVerificationChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        _context.PhoneVerificationChallenges.Add(ToRecord(challenge));
        return Task.CompletedTask;
    }

    public async Task<PhoneVerificationChallenge?> GetLatestAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        // Tracked on purpose: UpdateAsync compares the concurrency tokens against the values
        // observed here, so a concurrent consumption between load and save is detected.
        PhoneVerificationChallengeRecord? record =
            await _context.PhoneVerificationChallenges
                .Where(challenge => challenge.PhoneNumber == phoneNumber.Value)
                .OrderByDescending(challenge => challenge.IssuedAt)
                .ThenByDescending(challenge => challenge.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public Task<int> CountIssuedSinceAsync(
        PhoneNumber phoneNumber,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        return _context.PhoneVerificationChallenges
            .AsNoTracking()
            .CountAsync(
                challenge =>
                    challenge.PhoneNumber == phoneNumber.Value &&
                    challenge.IssuedAt >= since,
                cancellationToken);
    }

    public Task UpdateAsync(
        PhoneVerificationChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        PhoneVerificationChallengeRecord record =
            _context.PhoneVerificationChallenges.Local
                .SingleOrDefault(candidate => candidate.Id == challenge.Id)
            ?? throw new InvalidOperationException(
                "The verification challenge must be loaded through this repository before it is updated.");

        _context.Entry(record).CurrentValues.SetValues(ToRecord(challenge));
        return Task.CompletedTask;
    }

    private static PhoneVerificationChallengeRecord ToRecord(
        PhoneVerificationChallenge challenge) =>
        new()
        {
            Id = challenge.Id,
            PhoneNumber = challenge.PhoneNumber.Value,
            CodeHash = challenge.CodeHash,
            IssuedAt = challenge.IssuedAt,
            ExpiresAt = challenge.ExpiresAt,
            FailedAttempts = challenge.FailedAttempts,
            ConsumedAt = challenge.ConsumedAt,
        };

    private static PhoneVerificationChallenge ToDomain(
        PhoneVerificationChallengeRecord record) =>
        PhoneVerificationChallenge.Restore(
            record.Id,
            PhoneNumber.Create(record.PhoneNumber),
            record.CodeHash,
            record.IssuedAt,
            record.ExpiresAt,
            record.FailedAttempts,
            record.ConsumedAt);
}
