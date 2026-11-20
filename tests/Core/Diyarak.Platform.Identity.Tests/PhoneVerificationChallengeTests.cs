using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Platform.Identity.Tests;

public sealed class PhoneVerificationChallengeTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static readonly PhoneNumber Phone =
        PhoneNumber.Create("+963912345678");

    [Fact]
    public void Issue_sets_expiry_from_lifetime_and_starts_open()
    {
        PhoneVerificationChallenge challenge = CreateChallenge();

        Assert.Equal(Now.AddMinutes(5), challenge.ExpiresAt);
        Assert.Equal(0, challenge.FailedAttempts);
        Assert.Null(challenge.ConsumedAt);
        Assert.True(challenge.IsOpen(Now));
    }

    [Fact]
    public void Matching_code_verifies_and_consumes_challenge()
    {
        PhoneVerificationChallenge challenge = CreateChallenge();

        PhoneVerificationOutcome outcome =
            challenge.Attempt(codeMatches: true, Now.AddMinutes(1));

        Assert.Equal(PhoneVerificationOutcome.Verified, outcome);
        Assert.Equal(Now.AddMinutes(1), challenge.ConsumedAt);
        Assert.False(challenge.IsOpen(Now.AddMinutes(1)));
    }

    [Fact]
    public void Consumed_challenge_cannot_be_reused()
    {
        PhoneVerificationChallenge challenge = CreateChallenge();
        challenge.Attempt(codeMatches: true, Now);

        Assert.Equal(
            PhoneVerificationOutcome.AlreadyUsed,
            challenge.Attempt(codeMatches: true, Now));
    }

    [Fact]
    public void Expired_challenge_rejects_even_matching_code()
    {
        PhoneVerificationChallenge challenge = CreateChallenge();

        Assert.Equal(
            PhoneVerificationOutcome.Expired,
            challenge.Attempt(codeMatches: true, Now.AddMinutes(5)));
        Assert.Null(challenge.ConsumedAt);
    }

    [Fact]
    public void Wrong_codes_lock_challenge_after_maximum_attempts()
    {
        PhoneVerificationChallenge challenge = CreateChallenge();

        for (int i = 1; i < PhoneVerificationChallenge.MaximumFailedAttempts; i++)
        {
            Assert.Equal(
                PhoneVerificationOutcome.InvalidCode,
                challenge.Attempt(codeMatches: false, Now));
        }

        Assert.Equal(
            PhoneVerificationOutcome.TooManyAttempts,
            challenge.Attempt(codeMatches: false, Now));
        Assert.Equal(
            PhoneVerificationOutcome.TooManyAttempts,
            challenge.Attempt(codeMatches: true, Now));
        Assert.Null(challenge.ConsumedAt);
    }

    [Fact]
    public void Issue_rejects_empty_identifier_and_non_positive_lifetime()
    {
        Assert.Throws<ArgumentException>(
            () => PhoneVerificationChallenge.Issue(Guid.Empty, Phone, "hash", Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PhoneVerificationChallenge.Issue(Guid.NewGuid(), Phone, "hash", Now, TimeSpan.Zero));
        Assert.Throws<ArgumentException>(
            () => PhoneVerificationChallenge.Issue(Guid.NewGuid(), Phone, " ", Now, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Restore_rejects_failed_attempts_above_maximum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PhoneVerificationChallenge.Restore(
                Guid.NewGuid(),
                Phone,
                "hash",
                Now,
                Now.AddMinutes(5),
                PhoneVerificationChallenge.MaximumFailedAttempts + 1,
                consumedAt: null));
    }

    [Fact]
    public void Phone_account_rejects_empty_user()
    {
        Assert.Throws<ArgumentException>(
            () => new PhoneAccount(Phone, Guid.Empty, Now));
    }

    private static PhoneVerificationChallenge CreateChallenge() =>
        PhoneVerificationChallenge.Issue(
            Guid.NewGuid(),
            Phone,
            "hash",
            Now,
            TimeSpan.FromMinutes(5));
}
