using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Platform.Identity.Application;

/// <summary>
/// Thrown by an <see cref="IOtpSender"/> when the code could not be delivered. The message must
/// never contain the code, the phone number or provider credentials.
/// </summary>
public sealed class OtpDeliveryException : Exception
{
    public OtpDeliveryException(string message)
        : base(message)
    {
    }

    public OtpDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public OtpDeliveryException()
    {
    }
}

/// <summary>Delivers a one-time code to a phone number (SMS, WhatsApp, ...).</summary>
public interface IOtpSender
{
    public Task SendAsync(
        PhoneNumber phoneNumber,
        string code,
        CancellationToken cancellationToken = default);
}

/// <summary>Issues platform access tokens for internal users.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>The <c>iss</c> value written into issued tokens.</summary>
    public string Issuer { get; }

    public IssuedAccessToken Issue(Guid userId);
}

public sealed record IssuedAccessToken(
    string Value,
    DateTimeOffset ExpiresAt);

public interface IPhoneVerificationChallengeRepository
{
    public Task AddAsync(
        PhoneVerificationChallenge challenge,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the most recently issued challenge for the phone number, if any.</summary>
    public Task<PhoneVerificationChallenge?> GetLatestAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default);

    public Task<int> CountIssuedSinceAsync(
        PhoneNumber phoneNumber,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    public Task UpdateAsync(
        PhoneVerificationChallenge challenge,
        CancellationToken cancellationToken = default);
}

public interface IPhoneAccountRepository
{
    public Task<PhoneAccount?> FindAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new phone account together with the external identity mapping that lets
    /// platform-issued tokens resolve to the account's user.
    /// </summary>
    public Task AddAsync(
        PhoneAccount account,
        ExternalIdentity tokenIdentity,
        CancellationToken cancellationToken = default);
}

/// <summary>Commits staged identity changes atomically.</summary>
public interface IIdentityUnitOfWork
{
    /// <returns><c>false</c> when a concurrent change (unique or concurrency conflict) prevented the commit.</returns>
    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
