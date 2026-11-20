using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Platform.Identity;

/// <summary>
/// A one-time code sent to a phone number to prove possession of that number.
/// The code itself is never stored; only a hash supplied by the application layer.
/// </summary>
public sealed class PhoneVerificationChallenge : AggregateRoot<Guid>
{
    public const int MaximumFailedAttempts = 5;

    private PhoneVerificationChallenge(
        Guid id,
        PhoneNumber phoneNumber,
        string codeHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        int failedAttempts,
        DateTimeOffset? consumedAt)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException(
                "A verification challenge must expire after it is issued.",
                nameof(expiresAt));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(failedAttempts);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            failedAttempts,
            MaximumFailedAttempts);

        PhoneNumber = phoneNumber;
        CodeHash = codeHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        FailedAttempts = failedAttempts;
        ConsumedAt = consumedAt;
    }

    public PhoneNumber PhoneNumber { get; }

    public string CodeHash { get; }

    public DateTimeOffset IssuedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static PhoneVerificationChallenge Issue(
        Guid id,
        PhoneNumber phoneNumber,
        string codeHash,
        DateTimeOffset issuedAt,
        TimeSpan lifetime)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Challenge identifier cannot be empty.",
                nameof(id));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            lifetime,
            TimeSpan.Zero);

        return new PhoneVerificationChallenge(
            id,
            phoneNumber,
            codeHash,
            issuedAt,
            issuedAt.Add(lifetime),
            failedAttempts: 0,
            consumedAt: null);
    }

    public static PhoneVerificationChallenge Restore(
        Guid id,
        PhoneNumber phoneNumber,
        string codeHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        int failedAttempts,
        DateTimeOffset? consumedAt) =>
        new(
            id,
            phoneNumber,
            codeHash,
            issuedAt,
            expiresAt,
            failedAttempts,
            consumedAt);

    public bool IsOpen(DateTimeOffset now) =>
        ConsumedAt is null &&
        FailedAttempts < MaximumFailedAttempts &&
        now < ExpiresAt;

    /// <summary>
    /// Records a verification attempt. <paramref name="codeMatches"/> is computed by the caller
    /// with a constant-time comparison of code hashes.
    /// </summary>
    public PhoneVerificationOutcome Attempt(
        bool codeMatches,
        DateTimeOffset now)
    {
        if (ConsumedAt is not null)
            return PhoneVerificationOutcome.AlreadyUsed;

        if (FailedAttempts >= MaximumFailedAttempts)
            return PhoneVerificationOutcome.TooManyAttempts;

        if (now >= ExpiresAt)
            return PhoneVerificationOutcome.Expired;

        if (!codeMatches)
        {
            FailedAttempts++;

            return FailedAttempts >= MaximumFailedAttempts
                ? PhoneVerificationOutcome.TooManyAttempts
                : PhoneVerificationOutcome.InvalidCode;
        }

        ConsumedAt = now;
        return PhoneVerificationOutcome.Verified;
    }
}
